﻿using System;
using System.Collections.Generic;
using DLCS.Model.Assets.NamedQueries;
using DLCS.Model.PathElements;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Orchestrator.Infrastructure.NamedQueries.Parsing;
using Xunit;

namespace Orchestrator.Tests.Infrastructure.NamedQueries
{
    public class IIIFNamedQueryParserTests
    {
        private readonly BaseNamedQueryParser<IIIFParsedNamedQuery> sut;
        private static readonly CustomerPathElement Customer = new(99, "test-customer");

        public IIIFNamedQueryParserTests()
        {
            sut = new IIIFNamedQueryParser(new NullLogger<IIIFNamedQueryParser>());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void GenerateParsedNamedQueryFromRequest_Throws_IfTemplateEmptyOrWhiteSpace(string template)
        {
            // Act
            Action action = () =>
                sut.GenerateParsedNamedQueryFromRequest<IIIFParsedNamedQuery>(Customer, null, template);
            
            // Assert
            action.Should()
                .ThrowExactly<ArgumentNullException>()
                .WithMessage("Value cannot be null. (Parameter 'namedQueryTemplate')");
        }

        [Theory]
        [InlineData("space=p1", "")]
        [InlineData("space=p1&s1=p2", "1")]
        [InlineData("space=p1&s1=p2&#=1", "")]
        public void GenerateParsedNamedQueryFromRequest_ReturnsFaultParsedNQ_IfTooFewParamsPassed(string template,
            string args)
        {
            // Act
            var result = sut.GenerateParsedNamedQueryFromRequest<IIIFParsedNamedQuery>(Customer, args, template);

            // Assert
            result.IsFaulty.Should().BeTrue();
            result.ErrorMessage.Should().StartWith("Not enough query arguments to satisfy template element parameter");
        }

        [Theory]
        [InlineData("space=p1x", "1")]
        [InlineData("space=param", "1")]
        [InlineData("space=p1&s1=pa2&#=1", "")]
        public void GenerateParsedNamedQueryFromRequest_ReturnsFaultParsedNQ_IfInvalidParameterArgPassed(string template,
            string args)
        {
            // Act
            var result = sut.GenerateParsedNamedQueryFromRequest<IIIFParsedNamedQuery>(Customer, args, template);

            // Assert
            result.IsFaulty.Should().BeTrue();
            result.ErrorMessage.Should().StartWith("Could not parse template element parameter");
        }
        
        [Theory]
        [InlineData("n1=p1", "not-an-int")]
        [InlineData("n1=p1&#=not-an-int", "")]
        public void GenerateParsedNamedQueryFromRequest_ReturnsFaultParsedNQ_IfNonNumberPassedForNumberArg(string template,
            string args)
        {
            // Act
            var result = sut.GenerateParsedNamedQueryFromRequest<IIIFParsedNamedQuery>(Customer, args, template);

            // Assert
            result.IsFaulty.Should().BeTrue();
            result.ErrorMessage.Should().StartWith("Input string was not in a correct format");
        }

        [Theory]
        [MemberData(nameof(ParseNamedQueries))]
        public void GenerateParsedNamedQueryFromRequest_SuccessfullyParses(string template, string args,
            ParsedNamedQuery expected, string explanation)
        {
            // Act
            var result = sut.GenerateParsedNamedQueryFromRequest<IIIFParsedNamedQuery>(Customer, args, template);
            
            // Assert
            result.Should().BeEquivalentTo(expected, explanation);
        }
        
        // Note: This is not a completely exhaustive list
        public static IEnumerable<object[]> ParseNamedQueries => new List<object[]>
        {
            new object[] { "space=p1", "10", new IIIFParsedNamedQuery(Customer) { Space = 10 }, "Space from param" },
            new object[] { "space=5", "", new IIIFParsedNamedQuery(Customer) { Space = 5 }, "Hardcoded value" },
            new object[] { "space=p1&#=10", "", new IIIFParsedNamedQuery(Customer) { Space = 10 }, "Space from template" },
            new object[]
            {
                "manifest=s1&spacename=p1", "10",
                new IIIFParsedNamedQuery(Customer) { SpaceName = "10", Manifest = ParsedNamedQuery.QueryMapping.String1 },
                "Spacename from param"
            },
            new object[]
            {
                "manifest=s1&canvas=n2&s1=p1&n1=p2&space=p3&#=1", "string-1/40",
                new IIIFParsedNamedQuery(Customer)
                {
                    String1 = "string-1", Number1 = 40, Space = 1, Manifest = ParsedNamedQuery.QueryMapping.String1,
                    Canvas = IIIFParsedNamedQuery.QueryMapping.Number2
                },
                "All params"
            },
            new object[]
            {
                "manifest=s1&sequence=n1&canvas=n2&s1=p1&n1=p2&space=p3&#=1", "string-1/40/10/100",
                new IIIFParsedNamedQuery(Customer)
                {
                    String1 = "string-1", Number1 = 40, Space = 10, Manifest = ParsedNamedQuery.QueryMapping.String1,
                    Canvas = IIIFParsedNamedQuery.QueryMapping.Number2
                },
                "Extra args are ignored"
            },
            new object[]
            {
                "manifest=s1&&n3=&canvas=n2&=10&s1=p1&n1=p2&space=p3&#=1", "string-1/40",
                new IIIFParsedNamedQuery(Customer)
                {
                    String1 = "string-1", Number1 = 40, Space = 1, Manifest = ParsedNamedQuery.QueryMapping.String1,
                    Canvas = IIIFParsedNamedQuery.QueryMapping.Number2
                },
                "Incorrect template pairs are ignored"
            },
        };
    }
}