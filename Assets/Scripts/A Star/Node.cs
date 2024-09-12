using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node {

	public enum NodeLayer
	{
		Green,
		Blue,
		None
	}

    public int iGridX;
    public int iGridY;

    public bool bIsObstacle;
    public NodeLayer bIsCrate;
    public NodeLayer bIsDot;
    public Vector3 vPosition;

    public Node ParentNode;

    public int igCost;
    public int ihCost;

    public int FCost { get { return igCost + ihCost; } }

    public Node(bool aBIsObstacle, NodeLayer aBIsCrate, NodeLayer aBIsDot, Vector3 a_vPos, int a_igridX, int a_igridY)//Constructor
    {
        bIsObstacle = aBIsObstacle;
        vPosition = a_vPos;
        iGridX = a_igridX;
        iGridY = a_igridY;
        bIsCrate = aBIsCrate;
        bIsDot = aBIsDot;
    }

}
