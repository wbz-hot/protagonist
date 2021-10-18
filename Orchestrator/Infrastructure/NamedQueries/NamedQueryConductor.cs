﻿using System.Linq;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Assets.NamedQueries;
using DLCS.Model.PathElements;
using Microsoft.Extensions.Logging;

namespace Orchestrator.Infrastructure.NamedQueries
{
    /// <summary>
    /// Manages orchestration of named query parameters to generate list of results.
    /// </summary>
    public class NamedQueryConductor
    {
        private readonly INamedQueryRepository namedQueryRepository;
        private readonly NamedQueryParserResolver namedQueryParserResolver;
        private readonly ILogger<NamedQueryConductor> logger;

        public NamedQueryConductor(INamedQueryRepository namedQueryRepository, 
            NamedQueryParserResolver namedQueryParserResolver,
            ILogger<NamedQueryConductor> logger)
        {
            this.namedQueryRepository = namedQueryRepository; 
            this.namedQueryParserResolver = namedQueryParserResolver;
            this.logger = logger;
        }

        /// <summary>
        /// Generate <see cref="NamedQueryResult{T}"/> from named query. 
        /// </summary>
        /// <param name="queryName">Name of NQ to use</param>
        /// <param name="customerPathElement">CustomerPathElement used in request</param>
        /// <param name="args">Collection of NQ args passed in url (e.g. /2/my-images/99</param>
        public async Task<NamedQueryResult<T>> GetNamedQueryResult<T>(string queryName,
            CustomerPathElement customerPathElement, string? args)
            where T : ParsedNamedQuery
        {
            var namedQuery = await namedQueryRepository.GetByName(customerPathElement.Id, queryName);
            if (namedQuery == null)
            {
                return NamedQueryResult<T>.Empty();
            }

            var parsedNamedQuery = ParseNamedQuery<T>(customerPathElement, args, namedQuery);
            if (parsedNamedQuery.IsFaulty)
            {
                logger.LogInformation("Received faulted ParseNQ for {QueryName} with {QueryArgs}", queryName, args);
                return new NamedQueryResult<T>(parsedNamedQuery, Enumerable.Empty<Asset>());
            }

            var matchingImages = await namedQueryRepository.GetNamedQueryResults(parsedNamedQuery);
            return new NamedQueryResult<T>(parsedNamedQuery, matchingImages);
        }

        private T ParseNamedQuery<T>(CustomerPathElement customerPathElement, string? args, NamedQuery? namedQuery)
            where T : ParsedNamedQuery
        {
            var namedQueryParser = namedQueryParserResolver(typeof(T) == typeof(IIIFParsedNamedQuery)
                ? NamedQueryType.IIIF
                : NamedQueryType.PDF);
            var parsedNamedQuery =
                namedQueryParser.GenerateParsedNamedQueryFromRequest<T>(customerPathElement, args, namedQuery.Template);
            return parsedNamedQuery;
        }
    }
}