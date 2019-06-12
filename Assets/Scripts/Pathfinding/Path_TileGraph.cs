using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class constructs a simple path-finding compatible graph
/// of our world. Each tile is a node. Each walkable neighbour
/// from a tile is linked via an edge connection.
/// </summary>
public class Path_TileGraph {

    public Dictionary<Tile, Path_Node<Tile>> nodes;

    public Path_TileGraph(World world)
    {
        // Loop through all tiles of the world
        // For each tile, create a node
        // Do we create nodes for non-floor tiles? No.
        // Do we create nodes for tiles that are complete unwalkable?  No.

        nodes = new Dictionary<Tile, Path_Node<Tile>>();

        for (int x = 0; x < world.Width; x++)
        {
            for (int y = 0; y < world.Height; y++)
            {
                Tile t = world.GetTileAt(x, y);

                if (t.movementCost > 0) // Tiles with move cost 0 are unwalkable
                {
                    Path_Node<Tile> n = new Path_Node<Tile>();
                    n.data = t;
                    nodes.Add(t, n);
                }
            }
        }

        Debug.Log("Path_TileGraph: Created " + nodes.Count + " nodes.");

        // Now loop through all nodes again
        // Create edges for neighbours

        int edgeCount = 0;

        foreach(Tile t in nodes.Keys)
        {
            Path_Node<Tile> n = nodes[t];

            List<Path_Edge<Tile>> edges = new List<Path_Edge<Tile>>();

            // Get a lis tof neighbours for the tile
            Tile[] neighbours = t.GetNeighbours(true);  // Note: some array spots could be null

            // If neighbour is walkable, create an edge to the relevant node.
            for (int i = 0; i < neighbours.Length; i++)
            {
                if (neighbours[i] != null && neighbours[i].movementCost > 0)
                {
                    // Neighbour exists and is walkable, create an edge
                    Path_Edge<Tile> e = new Path_Edge<Tile>();
                    e.cost = neighbours[i].movementCost;
                    e.node = nodes[neighbours[i]];
                    edges.Add(e);

                    edgeCount++;
                }
            }

            n.edges = edges.ToArray();
        }

        Debug.Log("Path_TileGraph: Created " + edgeCount + " edges.");
    }

}
