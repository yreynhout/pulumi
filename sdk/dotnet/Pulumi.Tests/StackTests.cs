// Copyright 2016-2019, Pulumi Corporation

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Pulumi.Serialization;
using Pulumi.Testing;
using Xunit;

namespace Pulumi.Tests
{
    public class StackTests
    {
        private class ValidStack : Stack
        {
            [Output("foo")]
            public Output<string> ExplicitName { get; set; }

            [Output]
            public Output<string> ImplicitName { get; set; }

            public ValidStack()
            {
                this.ExplicitName = Output.Create("bar");
                this.ImplicitName = Output.Create("buzz");
            }
        }

        [Fact]
        public async Task ValidStackInstantiationSucceeds()
        {
            // Arrange
            ImmutableDictionary<string, object>? outputs = null;

            var mock = new Mock<IMocks>();
            mock.Setup(d => d.ResourceOutputsAsync(It.IsAny<string>(), It.IsAny<ImmutableDictionary<string, object>>()))
                .Callback((string _, ImmutableDictionary<string, object> o) => outputs = o);

            // Act
            var result = await Deployment.TestAsync<ValidStack>(mock.Object);

            // Assert
            Assert.Equal(0, result);
            
            Assert.NotNull(outputs);
            Assert.Equal(2, outputs!.Count);
            Assert.Equal("bar", outputs["foo"]);
            Assert.Equal("buzz", outputs["ImplicitName"]);
        }

        private class NullOutputStack : Stack
        {
            [Output("foo")]
            public Output<string>? Foo { get; }
        }

        [Fact]
        public async Task StackWithNullOutputsThrows()
        {
            string? loggedError = null;
            var mock = new Mock<IMocks>();
            mock.Setup(m => m.HandleErrorAsync(It.IsAny<string>())).Callback((string v) => loggedError = v);
            
            await Deployment.TestAsync<NullOutputStack>(mock.Object);
            
            Assert.NotNull(loggedError);
            Assert.Contains("Foo", loggedError);
        }

        private class InvalidOutputTypeStack : Stack
        {
            [Output("foo")]
            public string Foo { get; set; }

            public InvalidOutputTypeStack()
            {
                this.Foo = "bar";
            }
        }

        [Fact]
        public async Task StackWithInvalidOutputTypeThrows()
        {
            string? loggedError = null;
            var mock = new Mock<IMocks>();
            mock.Setup(m => m.HandleErrorAsync(It.IsAny<string>())).Callback((string v) => loggedError = v);
            
            await Deployment.TestAsync<InvalidOutputTypeStack>(mock.Object);
            
            Assert.NotNull(loggedError);
            Assert.Contains("Foo was not an Output", loggedError);
        }
    }
}
