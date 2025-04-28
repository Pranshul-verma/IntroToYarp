using Yarp.ReverseProxy.LoadBalancing;
using Yarp.ReverseProxy.Model;
using static Yarp.ReverseProxy.Health.HealthCheckConstants;
using LoadBalancerMain.LoadBalancerStrategy;
using System.Drawing;

namespace ApiGateway.LoadBalancerPolicy
{
    public class RoundRobinPolicy : ILoadBalancingPolicy
    {
        public string Name => "RoundRobinPolicy";
        List<DestinationState> destinationList = new List<DestinationState>();
        int size = 0;
        int position = 0;
        public DestinationState? PickDestination(HttpContext context, ClusterState cluster, IReadOnlyList<DestinationState> availableDestinations)
        {
            if (destinationList.Count() == 0)
            {
                foreach (var item in availableDestinations)
                {
                    size = destinationList.Count;
                    destinationList.Add(item);
                }
            }
            return Next();

        }

        private DestinationState? Next()
        {
            if (size == 1)
                return destinationList[0];

            var mod = position % size;
            Interlocked.Increment(ref position);
            return destinationList[position];
        }
    }
}
