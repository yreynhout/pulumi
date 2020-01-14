using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pulumi.Testing
{
    public interface IMocks
    {
        Task<object> CallAsync(string token, object args, string? provider);
    }
    
    class MockMonitor : IMonitor
    {
        private readonly IMocks mocks;
        public readonly List<string> LoggedMessages = new List<string>();
        
        public MockMonitor(IMocks mocks)
        {
            this.mocks = mocks;
        }

        public Task<global::Pulumirpc.InvokeResponse> InvokeAsync(global::Pulumirpc.InvokeRequest request)
            => throw new NotImplementedException();

        public Task<global::Pulumirpc.ReadResourceResponse> ReadResourceAsync(
            global::Pulumirpc.ReadResourceRequest request)
            => throw new NotImplementedException();

        public Task<global::Pulumirpc.RegisterResourceResponse> RegisterResourceAsync(
            global::Pulumirpc.RegisterResourceRequest request)
            => throw new NotImplementedException();

        public Task RegisterResourceOutputsAsync(
            global::Pulumirpc.RegisterResourceOutputsRequest request)
            => Task.CompletedTask;

        public Task LogAsync(global::Pulumirpc.LogRequest request)
        {
            this.LoggedMessages.Add(request.Message);
            return Task.CompletedTask;
        }

        public Task<global::Pulumirpc.SetRootResourceResponse> SetRootResourceAsync(
            global::Pulumirpc.SetRootResourceRequest request)
            => throw new NotImplementedException();

        public Task<global::Pulumirpc.GetRootResourceResponse> GetRootResourceAsync(
            global::Pulumirpc.GetRootResourceRequest request)
            => throw new NotImplementedException();
    }

}
