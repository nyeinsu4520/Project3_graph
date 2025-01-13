using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

public class Graph
{
    // Method to create an adjacency list from the input file
    public static Dictionary<string, List<string>> CreateGraphFromFile(string fileName)
    {
        var graph = new Dictionary<string, List<string>>();

        if (File.Exists(fileName))
        {
            using (var sr = new StreamReader(fileName))
            {
                while (!sr.EndOfStream)
                {
                    string[] edge = sr.ReadLine().Split(' '); // Read edge from file
                    string node1 = edge[0];
                    string node2 = edge[1];

                    // Add node1 -> node2
                    if (!graph.ContainsKey(node1))
                        graph[node1] = new List<string>();
                    graph[node1].Add(node2);

                    // Add node2 -> node1 (since it's undirected)
                    if (!graph.ContainsKey(node2))
                        graph[node2] = new List<string>();
                    graph[node2].Add(node1);
                }
            }
        }
        return graph;
    }

    //Method to create a graph from a file
    public static Dictionary<string, List<Tuple<string,int>>> CreateWeightGraphFromFile(string FileName)
    {
        var weightedgraph = new Dictionary<string, List<Tuple<string, int>>>();

        if (File.Exists(FileName))
        {
            using (var sr = new StreamReader(FileName))
            {
                while (!sr.EndOfStream)
                {
                    string[] edge = sr.ReadLine().Split(' ');
                    if (edge.Length != 3)
                    {
                        Console.WriteLine("Skipping invalid line (should have 3 elements): " + string.Join(" ", edge));
                        continue; // Skip this line if it doesn't have 3 elements
                    }
                    
                    string node1 = edge[0];
                    string node2 = edge[1];
                    int weight = int.Parse(edge[2]);

                    // Add node1 -> node2 with weight
                    if (!weightedgraph.ContainsKey(node1))
                        weightedgraph[node1] = new List<Tuple<string, int>>();
                    weightedgraph[node1].Add(new Tuple<string, int>(node2, weight));

                    // Add node2 -> node1 with weight (undirected graph)
                    if (!weightedgraph.ContainsKey(node2))
                        weightedgraph[node2] = new List<Tuple<string, int>>();
                    weightedgraph[node2].Add(new Tuple<string, int>(node1, weight));
                }
            }
        }
        return weightedgraph;
    }

    // BFS algorithm for unweighted graph
    public static Dictionary<string, int> BFS(Dictionary<string, List<string>> graph, string startNode)
    {
        var distances = new Dictionary<string, int>();
        var queue = new Queue<string>();

        // Initialize distances with infinity
        foreach (var node in graph.Keys)
        {
            distances[node] = int.MaxValue;
        }

        // Start BFS from the startNode
        distances[startNode] = 0;
        queue.Enqueue(startNode);

        while (queue.Count > 0)
        {
            string currentNode = queue.Dequeue();

            // Visit all neighbors
            foreach (var neighbor in graph[currentNode])
            {
                if (distances[neighbor] == int.MaxValue) // Not visited
                {
                    distances[neighbor] = distances[currentNode] + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return distances;
    }


    // Method to find the shortest path between start and target node using BFS
    public static void FindShortestPath(Dictionary<string, List<string>> graph, string startnode, string targetNode)
    {
        var distances = BFS(graph, startnode);

        if (distances[targetNode] == int.MaxValue)
        {
            Console.WriteLine($"There is no path between {startnode} and {targetNode}. They are disconnected.");
        }
        else
        {
            Console.WriteLine($"The shortest path from {startnode} to {targetNode} is {distances[targetNode]} edges long.");
        }
    }


    public static Dictionary<string, double> CalculateInfluenceScores(Dictionary<string, List<string>> graph)
    {
        var influenceScores = new Dictionary<string, double>();
        int n = graph.Keys.Count;

        foreach (var node in graph.Keys)
        {
            var distances = BFS(graph, node);
            int sumOfDistances = distances.Values.Sum();

            // Avoid division by zero for isolated nodes
            if (sumOfDistances > 0)
            {
                influenceScores[node] = (double)(n - 1) / sumOfDistances;
            }
            else
            {
                influenceScores[node] = 0.0;
            }
        }

        return influenceScores;
    }


    // Dijkstra's algorithm to find the shortest path from the start node to all others
    public static Dictionary<string, int> Dijkstra(Dictionary<string, List<Tuple<string, int>>> weightedgraph, string weightedstartNode)
    {
        var weightdistance = new Dictionary<string, int>();
        var priorityQueue = new SortedSet<KeyValuePair<int, string>>(Comparer<KeyValuePair<int, string>>.Create((x, y) => x.Key == y.Key ? x.Value.CompareTo(y.Value) : x.Key.CompareTo(y.Key)));

        // Initialize distances with infinity for all nodes except the start node
        foreach (var node in weightedgraph.Keys)
        {
            weightdistance[node] = int.MaxValue;  // Infinite distance initially
        }
        weightdistance[weightedstartNode] = 0;  // Distance to the start node is 0

        // Add the start node to the priority queue
        priorityQueue.Add(new KeyValuePair<int, string>(0, weightedstartNode));

        while (priorityQueue.Count > 0)
        {
            // Get the node with the smallest distance
            var current = priorityQueue.Min;
            priorityQueue.Remove(current);  // Remove the node with the smallest distance

            string currentNode = current.Value;

            // Explore neighbors
            foreach (var neighbor in weightedgraph[currentNode])
            {
                string neighborNode = neighbor.Item1;
                int weight = neighbor.Item2;

                int newDist = weightdistance[currentNode] + weight;

                if (newDist < weightdistance[neighborNode])  // Found a shorter path
                {
                    weightdistance[neighborNode] = newDist;

                    // Remove the old entry if it exists in the priority queue
                    priorityQueue.RemoveWhere(x => x.Value == neighborNode);

                    // Add the neighbor to the priority queue with the updated distance
                    priorityQueue.Add(new KeyValuePair<int, string>(newDist, neighborNode));
                }
            }
        }

        return weightdistance;
    }

    public static Dictionary<string, double> CalculateWeightedInfluenceScore(Dictionary<string, List<(string, int)>> graph, Dictionary<string, int> shortestDistances, int totalNodes)
    {
        var influenceScores = new Dictionary<string, double>();

        foreach (var node in graph.Keys)
        {
            double totalDistance = 0;
            int reachableNodes = 0;

            foreach (var distance in shortestDistances)
            {
                if (distance.Key != node && distance.Value != int.MaxValue) // Ignore unreachable nodes
                {
                    totalDistance += distance.Value;
                    reachableNodes++;
                }
            }

            // If no reachable nodes, influence score is zero
            if (reachableNodes == 0)
            {
                influenceScores[node] = 0;
            }
            else
            {
                influenceScores[node] = (totalNodes - 1) / totalDistance;
            }
        }

        return influenceScores;
    }

    // Find connected components (using BFS)
    public static List<List<string>> FindConnectedComponents(Dictionary<string, List<string>> graph)
    {
        var visited = new HashSet<string>();
        var connectedComponents = new List<List<string>>();

        foreach (var node in graph.Keys)
        {
            if (!visited.Contains(node))
            {
                var component = new List<string>();
                var queue = new Queue<string>();
                queue.Enqueue(node);
                visited.Add(node);

                while (queue.Count > 0)
                {
                    string currentNode = queue.Dequeue();
                    component.Add(currentNode);

                    foreach (var neighbor in graph[currentNode])
                    {
                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
                connectedComponents.Add(component);
            }
        }
        return connectedComponents;
    }

    // Handle disconnected nodes (nullptr for disconnected)
    public static Dictionary<string, List<string>> HandleDisconnectedGraph(Dictionary<string, List<string>> graph)
    {
        var allNodes = new HashSet<string>(graph.Keys);

        // Check for disconnected nodes
        foreach (var node in allNodes)
        {
            if (!graph.ContainsKey(node) || graph[node].Count == 0)
            {
                // Explicitly mark disconnected nodes as having no neighbors (nullptr)
                graph[node] = new List<string> { "nullptr" }; // Using "nullptr" as a placeholder for disconnected
            }
        }

        return graph;
    }


    // Main method
    public static void Main(string[] args)
    {
        // File path for the edge list
        string fileName = "C:\\Users\\DELL\\source\\repos\\Project3_Graph\\Project3_Graph\\unweighted_graph.txt";
        string FileName = "C:\\Users\\DELL\\source\\repos\\Project3_Graph\\Project3_Graph\\weighted_graph.txt";
        string roadEuroRoadFile = "C:\\Users\\DELL\\source\\repos\\Project3_Graph\\Project3_Graph\\road-euroroad.txt";

        // Create graph from file
        var graph = CreateGraphFromFile(fileName);
        var weightedgraph = CreateWeightGraphFromFile(FileName);
        var roadEuroRoadGraph = CreateGraphFromFile(roadEuroRoadFile);

        var cleanedGraph = HandleDisconnectedGraph(roadEuroRoadGraph);
        var connectedComponents = FindConnectedComponents(cleanedGraph);

       
        // Print adjacency list
        Console.WriteLine("Adjacency List:");
        foreach (var node in graph)
        {
            Console.WriteLine($"{node.Key} -> {string.Join(", ", node.Value)}");
        }

        string startNode;
        while (true) { 
        // Perform BFS from a starting node
        Console.WriteLine("\nEnter the starting node for BFS:");
        startNode = Console.ReadLine();

        if (graph.ContainsKey(startNode))
        {
            break;
        }
        else
        {
            Console.WriteLine($"Node {startNode} does not exist in the graph.Please try again.");
                
        }
        }
        var distances = BFS(graph, startNode);

        // Print shortest distances
        Console.WriteLine("\nShortest Distances from the starting node:");
        foreach (var node in distances)
        {
            string distance = node.Value == int.MaxValue ? "Unreachable" : node.Value.ToString();
            Console.WriteLine($"{node.Key}: {distance}");
        }

        Console.WriteLine("\nInfluence Scores (Unweighted Graph):");
        var influenceScores = CalculateInfluenceScores(graph);
        foreach (var node in influenceScores)
        {
            Console.WriteLine($"{node.Key}: {node.Value:F2}");
        }

        // Print the graph as an adjacency list
        Console.WriteLine("Adjacency List:");
        foreach (var node in weightedgraph)
        {
            Console.WriteLine($"{node.Key} -> {string.Join(", ", node.Value)}");
        }

        // Ask for the starting node
        string weightedstartNode;
        while (true) 
        { 
        Console.WriteLine("\nEnter the starting node for Dijkstra:");
        weightedstartNode = Console.ReadLine();

            if (weightedgraph.ContainsKey(weightedstartNode))
            {
                break;
            }
            else
            {
                Console.WriteLine($"Node {weightedstartNode} does not exist in the graph. Please try again.");
            }
           
        }
        // Perform Dijkstra's algorithm
        var weightdistances = Dijkstra(weightedgraph, weightedstartNode);

        // Print the shortest distances from the starting node
        Console.WriteLine("\nShortest Distances from the starting node:");
        foreach (var node in weightdistances)
        {
            string weightdistance = node.Value == int.MaxValue ? "Unreachable" : node.Value.ToString();
            Console.WriteLine($"{node.Key}: {weightdistance}");
        }


        // Perform Dijkstra's algorithm for the starting node
        var weightedDistances = Dijkstra(weightedgraph, weightedstartNode);

        // Total number of nodes in the graph
        int totalNodes = weightedgraph.Keys.Count;

        // Convert Tuple<string, int> to (string, int)
        var convertedGraph = weightedgraph.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Select(tuple => (tuple.Item1, tuple.Item2)).ToList()
        );

        // Calculate weighted influence scores
        var weightedInfluenceScores = CalculateWeightedInfluenceScore(convertedGraph, weightdistances, totalNodes);


        // Print the weighted influence scores
        Console.WriteLine("\nWeighted Influence Scores:");
        foreach (var node in weightedInfluenceScores)
        {
            Console.WriteLine($"{node.Key}: {node.Value:F2}"); // Format to 2 decimal places
        }

        string startnode;
        while (true) {
        // Ask for the starting node and target node
        Console.WriteLine("\nEnter the starting node for the Road Euro Road Graph:");
        startnode = Console.ReadLine();
        // Check if the nodes exist in the graph
        if (cleanedGraph.ContainsKey(startnode))
            {
                break;
            }
        else
            {

                Console.WriteLine($"Node {startnode} does not exist in the graph.");
         
            }
        }
        string targetNode;
        while (true) {

        Console.WriteLine("Enter the target node:");
        targetNode = Console.ReadLine();
        if (cleanedGraph.ContainsKey(targetNode))
        {
            break;
        }
        else
        {
            Console.WriteLine($"Node {targetNode} does not exist in the graph.");
            
        }
        }
        FindShortestPath(roadEuroRoadGraph, startnode, targetNode);
        



    }
}




