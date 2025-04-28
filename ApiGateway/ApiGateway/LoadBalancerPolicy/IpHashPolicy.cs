using Microsoft.AspNetCore.Hosting.Server;
using System.Drawing;
using Yarp.ReverseProxy.LoadBalancing;
using Yarp.ReverseProxy.Model;

namespace ApiGateway.LoadBalancerPolicy
{
    public class IpHashPolicy : ILoadBalancingPolicy
    {
        public string Name => "IpHashLBpolicy";
        List<DestinationState> destinationList ;
        
        public DestinationState? PickDestination(HttpContext context, ClusterState cluster, IReadOnlyList<DestinationState> availableDestinations)
        {
            destinationList = new List<DestinationState>();
            foreach (var item in availableDestinations)
            {                
                destinationList.Add(item);
            }

            return GetNextServer(destinationList, context.Request.Headers.FirstOrDefault(x => x.Key == "X-Forwarded-For").Value);
        }

        private DestinationState? GetNextServer(List<DestinationState> destinationList, string clientIp)
        {
            var hascode = clientIp.GetHashCode();
            int serverIndex = Math.Abs(hascode) % destinationList.Count;
            return destinationList[serverIndex];
        }
    }
}
