using System.Collections.Generic;
using UnityEngine;

namespace Polyhydra.Core
{
    public class FaceLoop : List<Face>
    {
        public static FaceLoop FromHalfEdge(Halfedge start)
        {
            // Attempt to find a loop of halfedges from a starting point
            // If a boundary is hit then go back to the start and add the other side
            // If a loop is found then return it
            // If a loop is not found then return null
            int count = 0;
            int limit = 1000;
            var loop = new FaceLoop();
            loop.Add(start.Face);
            var current = start;
            do
            {
                if (current.Pair==null || current.Pair.Face.Sides==3)
                {
                    // We've hit a boundary
                    // Quit this loop so we can go back and add the other side
                    break;
                }
                current = current.Pair.OppositeByIndex;
                loop.Add(current.Face);
                if (current.Face == start.Face) return loop;
                count++;
            } while (count < limit);

            // We hit a boundary so the loop is incomplete
            // However we can add faces in the other direction until we hit another boundary
            current = start.OppositeByIndex.Pair;
            if (current == null) return loop;
            do
            {
                loop.Insert(0, current.Face);
                current = current.OppositeByIndex;
                if (current.Face == start.Face)
                {
                    Debug.LogWarning($"Found a boundary in the other direction but looped this side");
                    return loop; // Shouldn't happen but hey
                }
                if (current.Pair==null || current.Pair.Face.Sides==3)
                {
                    // Hit the other boundary
                    return loop;
                }
                count++;
            } while (count < limit);
            Debug.LogWarning("Unexpected loop termination");
            return loop;
        }
    }
}