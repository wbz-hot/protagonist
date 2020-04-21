using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DLCS.Core.Tests
{
    public class CallChainTests
    {
        [Fact]
        public void ExecuteInSequence_Throws_IfArgsNull()
        {
            // Act
            Func<Task> action = () => CallChain.ExecuteInSequence(null);
            
            // Assert
            action.Should().Throw<ArgumentNullException>();
        }
        
        [Fact]
        public async Task ExecuteInSequence_ExecutesAllInSequence()
        {
            // Arrange
            var callOrder = new List<int>();
            var expected = new List<int> {1, 2, 3};
            int counter = 0;

            async Task<bool> Command()
            {
                await Task.Delay(200);
                Interlocked.Increment(ref counter);
                callOrder.Add(counter);
                return true;
            }

            // Act
            var result = await CallChain.ExecuteInSequence(Command, Command, Command);
            
            // Assert
            result.Should().BeTrue();
            callOrder.Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public async Task ExecuteInSequence_ExecutesUntilFailure()
        {
            // Arrange
            var callOrder = new List<int>();
            var expected = new List<int> {1, 2};
            int counter = 0;

            async Task<bool> Command()
            {
                await Task.Delay(200);

                // bail after 2nd iteration
                if (counter == 2) return false;

                Interlocked.Increment(ref counter);
                callOrder.Add(counter);
                return true;
            }

            // Act
            var result = await CallChain.ExecuteInSequence(Command, Command, Command, Command);
            
            // Assert
            result.Should().BeFalse();
            callOrder.Should().BeEquivalentTo(expected);
        }
    }
}