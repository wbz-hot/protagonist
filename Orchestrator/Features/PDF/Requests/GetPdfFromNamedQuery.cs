﻿using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets.NamedQueries;
using MediatR;
using Orchestrator.Infrastructure.NamedQueries.Models;
using Orchestrator.Infrastructure.NamedQueries.Requests;

namespace Orchestrator.Features.PDF.Requests
{
    /// <summary>
    /// Mediatr request for generating PDF via named query
    /// </summary>
    public class GetPdfFromNamedQuery : IBaseNamedQueryRequest, IRequest<PersistedProjectionFromNamedQuery>
    {
        public string CustomerPathValue { get; }
        public string NamedQuery { get; }
        public string? NamedQueryArgs { get; }
        
        public GetPdfFromNamedQuery(string customerPathValue, string namedQuery, string? namedQueryArgs)
        {
            CustomerPathValue = customerPathValue;
            NamedQuery = namedQuery;
            NamedQueryArgs = namedQueryArgs;
        }
    }
    
    public class GetPdfFromNamedQueryHandler : IRequestHandler<GetPdfFromNamedQuery, PersistedProjectionFromNamedQuery>
    {
        private readonly StoredNamedQueryService storedNamedQueryService;
        private readonly NamedQueryResultGenerator namedQueryResultGenerator;
        private readonly IPdfCreator pdfCreator;

        public GetPdfFromNamedQueryHandler(
            StoredNamedQueryService storedNamedQueryService,
            NamedQueryResultGenerator namedQueryResultGenerator,
            IPdfCreator pdfCreator)
        {
            this.storedNamedQueryService = storedNamedQueryService;
            this.namedQueryResultGenerator = namedQueryResultGenerator;
            this.pdfCreator = pdfCreator;
        }

        public async Task<PersistedProjectionFromNamedQuery> Handle(GetPdfFromNamedQuery request,
            CancellationToken cancellationToken)
        {
            var namedQueryResult =
                await namedQueryResultGenerator.GetNamedQueryResult<PdfParsedNamedQuery>(request);

            if (namedQueryResult.ParsedQuery == null)
                return new PersistedProjectionFromNamedQuery(PersistedProjectionStatus.NotFound);
            if (namedQueryResult.ParsedQuery is { IsFaulty: true })
                return PersistedProjectionFromNamedQuery.BadRequest();

            var pdfResult = await storedNamedQueryService.GetResults(namedQueryResult,
                (query, images) => pdfCreator.CreatePdf(query, images));

            return pdfResult.Status == PersistedProjectionStatus.InProcess
                ? new PersistedProjectionFromNamedQuery(PersistedProjectionStatus.InProcess)
                : new PersistedProjectionFromNamedQuery(pdfResult.Stream, pdfResult.Status);
        }
    }
}