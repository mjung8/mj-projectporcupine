using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;
using System.Linq;

public class Path_AStar
{

    Queue<Tile> path;

    public Path_AStar(World world, Tile tileStart, Tile tileEnd)
    {
        // Check to see if there's a valid tile graph
        if (world.tileGraph == null)
        {
            world.tileGraph = new Path_TileGraph(world);
        }

        // Dictionary of all valid, walkable nodes
        Dictionary<Tile, Path_Node<Tile>> nodes = world.tileGraph.nodes;

        Path_Node<Tile> start = nodes[tileStart];
        Path_Node<Tile> goal = nodes[tileEnd];

        // Make sure start/end tiles are in the list of nodes
        if (nodes.ContainsKey(tileStart) == false)
        {
            Debug.LogError("Path_AStar: the starting tile isn't in the list of nodes.");
            return;
        }

        if (nodes.ContainsKey(tileEnd) == false)
        {
            Debug.LogError("Path_AStar: the ending tile isn't in the list of nodes.");
            return;
        }

        // Mostly following wikipedia A* search algorithm
        List<Path_Node<Tile>> ClosedSet = new List<Path_Node<Tile>>();

        //List<Path_Node<Tile>> OpenSet = new List<Path_Node<Tile>>();
        //OpenSet.Add(start);

        SimplePriorityQueue<Path_Node<Tile>> OpenSet = new SimplePriorityQueue<Path_Node<Tile>>();
        OpenSet.Enqueue(start, 0);

        Dictionary<Path_Node<Tile>, Path_Node<Tile>> Came_From = new Dictionary<Path_Node<Tile>, Path_Node<Tile>>();

        Dictionary<Path_Node<Tile>, float> g_score = new Dictionary<Path_Node<Tile>, float>();
        foreach (Path_Node<Tile> n in nodes.Values)
        {
            g_score[n] = Mathf.Infinity;
        }
        g_score[start] = 0;

        Dictionary<Path_Node<Tile>, float> f_score = new Dictionary<Path_Node<Tile>, float>();
        foreach (Path_Node<Tile> n in nodes.Values)
        {
            f_score[n] = Mathf.Infinity;
        }
        f_score[start] = heuristic_cost_estimate(start, goal);

        while (OpenSet.Count > 0)
        {
            Path_Node<Tile> current = OpenSet.Dequeue();
            if (current == goal)
            {
                // We have reached our goal.
                // Convert this into a sequence of tiles to walk on.
                // Then end this constructor function.
                reconstruct_path(Came_From, current);
                return;
            }

            ClosedSet.Add(current);

            foreach (Path_Edge<Tile> edge_neighbour in current.edges)
            {
                Path_Node<Tile> neighbour = edge_neighbour.node;

                if (ClosedSet.Contains(neighbour) == true)
                {
                    continue;
                }

                float tentative_g_score = g_score[current] + dist_between(current, neighbour);

                if (OpenSet.Contains(neighbour) && tentative_g_score >= g_score[neighbour])
                    continue;

                Came_From[neighbour] = current;
                g_score[neighbour] = tentative_g_score;
                f_score[neighbour] = g_score[neighbour] + heuristic_cost_estimate(neighbour, goal);

                if (OpenSet.Contains(neighbour) == false)
                {
                    OpenSet.Enqueue(neighbour, f_score[neighbour]);
                }
            } // foreach neighbour
        }  // while

        // If we're here, we've gone through the entire OpenSet
        // without reaching a point where current == goal.
        // This happens when there is no path from start to goal.

        return;

    }

    float heuristic_cost_estimate(Path_Node<Tile> a, Path_Node<Tile> b)
    {
        return Mathf.Sqrt(
            Mathf.Pow(a.data.X - b.data.X, 2) +
            Mathf.Pow(a.data.Y - b.data.Y, 2)
        );
    }

    float dist_between(Path_Node<Tile> a, Path_Node<Tile> b)
    {
        // We can make assumptions because we know we're working on a grid

        // Horizontal/Vertical neighbours have distance of 1
        if (Mathf.Abs(a.data.X - b.data.X) + Mathf.Abs(a.data.Y - b.data.Y) == 1)
        {
            return 1f;
        }

        // Diagonal neighbours have 1.41421356237
        if (Mathf.Abs(a.data.X - b.data.X) == 1 && Mathf.Abs(a.data.Y - b.data.Y) == 1)
        {
            return 1.41421356237f;
        }

        return Mathf.Sqrt(
            Mathf.Pow(a.data.X - b.data.X, 2) +
            Mathf.Pow(a.data.Y - b.data.Y, 2)
        );
    }

    void reconstruct_path(Dictionary<Path_Node<Tile>, Path_Node<Tile>> Came_From, Path_Node<Tile> current)
    {
        // Here, current is the goal
        // So walk backwards through Came_From map until we reach the end
        // which will be the starting node.
        Queue<Tile> total_path = new Queue<Tile>();
        total_path.Enqueue(current.data); // This final step is the path to the goal

        while (Came_From.ContainsKey(current))
        {
            // Came_From is a map where the key => value relation is
            // some_node => we_got_there_from_this_node
            current = Came_From[current];
            total_path.Enqueue(current.data);
        }

        // At this point, total_path is a queue that is running backwards
        // from the end tile to the starting tile, so reverse it

        path = new Queue<Tile>(total_path.Reverse());

    }

    public Tile GetNextTile()
    {
        return path.Dequeue();
    }
}
