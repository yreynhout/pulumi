using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Pulumi.Serialization;
using Pulumirpc;

namespace Pulumi.Testing
{
    /// <summary>
    /// Hooks to mock the engine and provide test doubles for offline unit testing of stacks.
    /// </summary>
    public interface IMocks
    {
        /// <summary>
        /// Invoked when a new resource is created by the program.
        /// </summary>
        /// <param name="type">Resource type name.</param>
        /// <param name="name">Resource name.</param>
        /// <param name="inputs">Dictionary of resource input properties.</param>
        /// <param name="provider">Provider.</param>
        /// <param name="id">Resource identifier.</param>
        /// <returns>A tuple of a resource identifier and resource state. State can be either a POCO
        /// or a dictionary bag.</returns>
        Task<(string id, object state)> NewResourceAsync(string type, string name,
            ImmutableDictionary<string, object> inputs, string? provider, string? id);

        /// <summary>
        /// Invoked when the program needs to call a provider to load data (e.g., to retrieve an existing
        /// resource).
        /// </summary>
        /// <param name="token">Function token.</param>
        /// <param name="args">Dictionary of input arguments.</param>
        /// <param name="provider">Provider.</param>
        /// <returns>Invocation result, can be either a POCO or a dictionary bag.</returns>
        Task<object> CallAsync(string token, ImmutableDictionary<string, object> args, string? provider);

        /// <summary>
        /// Invoked when an error is raised by the program execution.
        /// </summary>
        /// <param name="message">Error message.</param>
        Task HandleErrorAsync(string message);

        /// <summary>
        /// Invoked when a resource registers its outputs.
        /// </summary>
        /// <param name="urn">Resource URN.</param>
        /// <param name="outputs">Outputs property bag.</param>
        Task ResourceOutputsAsync(string urn, ImmutableDictionary<string, object> outputs);
    }
    
    class MockMonitor : IMonitor
    {
        private readonly IMocks _mocks;
        private readonly Serializer _serializer = new Serializer();
        private string? _rootResourceUrn = null;
        
        public MockMonitor(IMocks mocks)
        {
            _mocks = mocks;
        }

        public async Task<InvokeResponse> InvokeAsync(InvokeRequest request)
        {
            var result = await _mocks.CallAsync(request.Tok, ToDictionary(request.Args), request.Provider);
            return new InvokeResponse { Return = await SerializeAsync(result) };
        }

        public async Task<ReadResourceResponse> ReadResourceAsync(ReadResourceRequest request)
        {
            var (id, state) = await _mocks.NewResourceAsync(request.Type, request.Name,
                ToDictionary(request.Properties), request.Provider, request.Id);
            return new ReadResourceResponse
            {
                Urn = NewUrn(request.Parent, request.Type, request.Name),
                Properties = await SerializeAsync(state)
            };
        }

        public async Task<RegisterResourceResponse> RegisterResourceAsync(RegisterResourceRequest request)
        {
            var (id, state) = await _mocks.NewResourceAsync(request.Type, request.Name, ToDictionary(request.Object),
                request.Provider, request.ImportId);
            return new RegisterResourceResponse
            {
                Id = id ?? request.ImportId,
                Urn = NewUrn(request.Parent, request.Type, request.Name),
                Object = await SerializeAsync(state)
            };
        }

        public Task RegisterResourceOutputsAsync(RegisterResourceOutputsRequest request)
            => _mocks.ResourceOutputsAsync(request.Urn, ToDictionary(request.Outputs));

        public Task LogAsync(LogRequest request)
        {
            if (request.Severity == LogSeverity.Error)
            {
                _mocks.HandleErrorAsync(request.Message);
            }
            
            return Task.CompletedTask;
        }

        public Task<SetRootResourceResponse> SetRootResourceAsync(SetRootResourceRequest request)
        {
            _rootResourceUrn = request.Urn;
            return Task.FromResult(new SetRootResourceResponse());
        }

        public Task<GetRootResourceResponse> GetRootResourceAsync(
            GetRootResourceRequest request)
            => Task.FromResult(new GetRootResourceResponse { Urn = _rootResourceUrn });
        
        private static string NewUrn(string parent, string type, string name)
        {
            if (!string.IsNullOrEmpty(parent)) 
            {
                var qualifiedType = parent.Split("::")[2];
                var parentType = qualifiedType.Split("$").First();
                type = parentType + "$" + type;
            }
            return "urn:pulumi:" + string.Join("::", new[] { Deployment.Instance.StackName, Deployment.Instance.ProjectName, type, name });
        }

        private static ImmutableDictionary<string, object> ToDictionary(Struct s)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, object>();
            foreach (var (key, value) in s.Fields)
            {
                var data = Deserializer.Deserialize(value);
                if (data.IsKnown && data.Value != null)
                {
                    builder.Add(key, data.Value);
                }
            }
            return builder.ToImmutable();
        }

        private async Task<Struct> SerializeAsync(object o)
        {
            var dict = (o as IDictionary<string, object>)?.ToImmutableDictionary()
                   ?? await _serializer.SerializeAsync("", o) as ImmutableDictionary<string, object>
                   ?? ImmutableDictionary<string, object>.Empty;
            return Serializer.CreateStruct(dict);
        }
    }

}
