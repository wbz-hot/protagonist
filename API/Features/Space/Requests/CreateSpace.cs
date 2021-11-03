using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Model;
using DLCS.Model.Customers;
using DLCS.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Features.Space.Requests
{
    /// <summary>
    /// See Deliverator: API/Architecture/Request/API/Entities/CustomerSpaces.cs
    /// </summary>
    public class CreateSpace : IRequest<DLCS.Repository.Entities.Space>
    {
        public string Name { get; set; }
        public int Customer { get; set; }
        public string? ImageBucket { get; set; } = string.Empty;
        public string? Tags { get; set; } = string.Empty;
        public string? Roles { get; set; } = string.Empty;
        public int? MaxUnauthorised { get; set; }

        public CreateSpace(int customer, string name)
        {
            Customer = customer;
            Name = name;
        }
    }


    public class CreateSpaceHandler : IRequestHandler<CreateSpace, DLCS.Repository.Entities.Space>
    {
        private readonly DlcsContext dbContext;
        private readonly ICustomerRepository customerRepository;
        private readonly IEntityCounterRepository entityCounterRepository;
        private readonly ILogger<CreateSpaceHandler> logger;

        public CreateSpaceHandler(
            DlcsContext dbContext,
            ICustomerRepository customerRepository,
            IEntityCounterRepository entityCounterRepository,
            ILogger<CreateSpaceHandler> logger)
        {
            this.dbContext = dbContext;
            this.customerRepository = customerRepository;
            this.entityCounterRepository = entityCounterRepository;
            this.logger = logger;
        }
        
        public async Task<DLCS.Repository.Entities.Space> Handle(CreateSpace request, CancellationToken cancellationToken)
        {
            await ValidateRequest(request);
            await EnsureSpaceNameNotTaken(request, cancellationToken);
            int newModelId = await GetIdForNewSpace(request.Customer);
            var space = await CreateNewSpace(request, cancellationToken, newModelId);
            await entityCounterRepository.Create(request.Customer,  "space-images", space.Id.ToString(), 1);
            await dbContext.SaveChangesAsync(cancellationToken);
            return space;
        }

        private async Task EnsureSpaceNameNotTaken(CreateSpace request, CancellationToken cancellationToken)
        {
            var existing = await dbContext.Spaces
                .Where(s => s.Customer == request.Customer)
                .SingleOrDefaultAsync(s => s.Name.Equals(request.Name, StringComparison.InvariantCultureIgnoreCase),
                    cancellationToken: cancellationToken);
            if (existing != null)
            {
                throw new BadRequestException("A space with this name (url part) already exists for this customer.");
            }
        }

        private async Task<DLCS.Repository.Entities.Space> CreateNewSpace(CreateSpace request, CancellationToken cancellationToken, int newModelId)
        {
            var space = new DLCS.Repository.Entities.Space
            {
                Id = newModelId,
                Name = request.Name,
                Created = DateTime.Now,
                ImageBucket = request.ImageBucket,
                Tags = request.Tags,
                Roles = request.Roles,
                MaxUnauthorised = request.MaxUnauthorised ?? -1
            };

            await dbContext.Spaces.AddAsync(space, cancellationToken);
            return space;
        }

        private async Task<int> GetIdForNewSpace(int requestCustomer)
        {
            int newModelId;
            DLCS.Repository.Entities.Space existingSpaceInCustomer;
            do
            {
                newModelId = Convert.ToInt32(entityCounterRepository.GetNext(requestCustomer, "space", requestCustomer.ToString()));
                existingSpaceInCustomer = await dbContext.Spaces.SingleOrDefaultAsync(s => s.Id == newModelId && s.Customer == requestCustomer);
            } while (existingSpaceInCustomer != null);

            return newModelId;
        }


        private async Task ValidateRequest(CreateSpace request)
        {
            var customer = await customerRepository.GetCustomer(request.Customer);
            if (customer == null)
            {
                throw new BadRequestException("Space must be created for an existing Customer.");
            }
        }
    }
}