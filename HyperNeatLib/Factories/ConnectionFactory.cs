using System;
using System.Collections.Generic;
using System.Linq;

using HyperNeatLib.Helpers;
using HyperNeatLib.Interfaces;
using HyperNeatLib.NEATImpl;

namespace HyperNeatLib.Factories
{
    public static class ConnectionFactory
    {
        private static Random random = new Random();

        public static List<IConnection> ConnectionList = new List<IConnection>();
        public static IConnection CreateConnection(INeuron source, INeuron target, double? weight = null, bool enabled = true, int? id = null)
        {
            if (!id.HasValue)
            {
                var existing =
                    ConnectionList.FirstOrDefault(
                        c => c.InputNode.Id == source.Id && c.OutputNode.Id == target.Id);

                //If the same mutation has already occured, but hasn't been split yet, reuse it (if it was split already, we would get problems if it's split again)
                if (existing != null)
                {
                    id = existing.Id;
                }
            }

            var connection = new Connection();
            connection.InputNode = source;
            connection.OutputNode = target;
            connection.Weight = weight ?? random.NextDouble() * 2 - 1;
            connection.IsEnabled = enabled;
            connection.Id = id ?? GenerationIdSingleton.Instance.NextConnectionGeneration;

            if (ConnectionList.All(c => c.Id != connection.Id))
            {
                ConnectionList.Add(connection);
            }

            return connection;
        }
    }
}