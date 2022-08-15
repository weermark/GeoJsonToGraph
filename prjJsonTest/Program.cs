using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Linq;

namespace prjJsonTest
{
    public class Crs
    {
        public string type { get; set; }
        public Properties properties { get; set; }
    }

    public class Feature
    {
        public string type { get; set; }
        public Properties properties { get; set; }
        public Geometry geometry { get; set; }
    }

    public class Geometry
    {
        public string type { get; set; }
        public List<List<List<double>>> coordinates { get; set; }
    }

    public class Properties
    {
        public string name { get; set; }
        public string osm_id { get; set; }
        public string highway { get; set; }
        public object waterway { get; set; }
        public object aerialway { get; set; }
        public object barrier { get; set; }
        public object man_made { get; set; }
        public int z_order { get; set; }
        public string other_tags { get; set; }
        public double z_zmean { get; set; }
    }

    public class Root
    {
        public string type { get; set; }
        public string name { get; set; }
        public Crs crs { get; set; }
        public List<Feature> features { get; set; }
    }

    public class DuplicateKeyComparer<TKey>
                :
             IComparer<TKey> where TKey : IComparable
    {
        #region IComparer<TKey> Members

        public int Compare(TKey x, TKey y)
        {
            int result = x.CompareTo(y);

            if (result == 0)
                return 1; // Handle equality as being greater. Note: this will break Remove(key) or
            else          // IndexOfKey(key) since the comparer never returns 0 to signal key equality
                return result;
        }

        #endregion
    }


    public class Node
    {
        public double x;
        public double y;
        public double z;
        public Node(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public override int GetHashCode()
        {
            return Convert.ToInt32(this.x * 1000);
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as Node);
        }
        public bool Equals(Node obj)
        {
            return obj != null && obj.x == this.x && obj.y == this.y && obj.z == this.z;
        }
    }
    public class Graph
    {
        Dictionary<Node, LinkedList<Node>> graph;

        public Graph()
        {
            graph = new Dictionary<Node, LinkedList<Node>>();
        }

        // doublic directed
        public void addEdge(Node n1, Node n2)
        {
            if (n1 == null) { return; }
            if (!graph.ContainsKey(n1))
            {
                graph.Add(n1, new LinkedList<Node>());
            }
            graph[n1].AddLast(n2);

            if (!graph.ContainsKey(n2))
            {
                graph.Add(n2, new LinkedList<Node>());
            }
            graph[n2].AddLast(n1);
        }
        public void printGraph()
        {
            foreach (var u in graph)
            {
                Console.Write(u.Key.x + " " + u.Key.y + " " + u.Key.z);
                foreach (var v in u.Value)
                {
                    Console.Write(" -> " + v.x + " " + v.y + " " + v.z);
                }
                Console.WriteLine();
            }
        }

        public double weight(Node n1, Node n2)
        {
            return (Math.Sqrt(Math.Pow(n2.x - n1.x, 2) + Math.Pow(n2.y - n1.y, 2) + Math.Pow(n2.z - n1.z, 2)));
        }

        public double dijkstra(Node start, Node end)
        {
            if (!graph.ContainsKey(start) && !graph.ContainsKey(end))
            {
                Console.WriteLine("the Node start or end isn't exist");
                return -1;
            }

            // init
            int len = graph.Count;
            // <postNode, preNode>
            Dictionary<Node, Node> preNodeDict = new Dictionary<Node, Node>(len);
            Dictionary<Node, double> distTo = new Dictionary<Node, double>(len);
            foreach (var node in graph)
            {
                preNodeDict.Add(node.Key, null);
                distTo.Add(node.Key, double.MaxValue);
            }
            distTo[start] = 0;

            SortedList<double, Node> sl = new SortedList<double, Node>(new DuplicateKeyComparer<double>());
            sl.Add(0, start);

            // run
            // if list.Count == 0, then terminate the algo 
            while (sl.Count != 0)
            {
                // get first ele
                var curPair = sl.First();
                Node curNode = curPair.Value;
                double curDist = curPair.Key;
                // delete first ele
                sl.RemoveAt(0);
                if (curNode.Equals(end))
                {
                    // print the Node on path 
                    Node tmp = end;
                    Console.WriteLine("-----------*Path*------------");
                    Console.WriteLine("************end**************");
                    while (tmp != null)
                    {
                        Console.WriteLine(tmp.x + " " + tmp.y + " " + tmp.z);
                        tmp = preNodeDict[tmp];
                    }
                    Console.WriteLine("************start************");
                    return curDist;
                }
                if (curDist > distTo[curNode])
                {
                    continue;
                }
                foreach (Node adjNode in graph[curNode])
                {
                    double distToNextNode = distTo[curNode] + weight(curNode, adjNode);
                    if (distTo[adjNode] > distToNextNode)
                    {
                        preNodeDict[adjNode] = curNode;
                        distTo[adjNode] = distToNextNode;
                        sl.Add(distToNextNode, adjNode);
                    }
                }
            }
            // start can't go to end, then return -1
            return -1;
        }

        public void JsonToGraph(string path)
        {
            using (StreamReader r = new StreamReader(path))
            {
                // read json
                string json = r.ReadToEnd();
                Root root = JsonConvert.DeserializeObject<Root>(json);
                // use to save last time's Node
                Node tmp = null;
                foreach (var feature in root.features)
                {
                    // filter
                    if (feature.properties.highway == null) { continue; }
                    // add data to graph
                    foreach (var coor in feature.geometry.coordinates[0])
                    {
                        this.addEdge(tmp, new Node(coor[0], coor[1], coor[2]));
                        tmp = new Node(coor[0], coor[1], coor[2]);
                    }
                    // null Node will not be add into graph
                    tmp = null;
                }
            }
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Graph g = new Graph();
            g.JsonToGraph("..//..//..//..//road_osm_json.json");
            Console.WriteLine("distance: " + g.dijkstra(
                new Node(177115.863063361495733, 2559149.205660121049732, 13.090000152587891)
                , new Node(173121.566283993364777, 2555118.290666623972356, 5.119999885559082)));
        }
    }
}
