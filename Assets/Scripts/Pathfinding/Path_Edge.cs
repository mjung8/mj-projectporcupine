﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path_Edge<T>
{
    // Cost to traverse this edge (cost to ENTER tile)
    public float cost;
    public Path_Node<T> node;
}
