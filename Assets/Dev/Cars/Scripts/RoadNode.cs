using System.Collections.Generic;
using UnityEngine;

public class RoadNode : MonoBehaviour
{
    [SerializeField] private List<RoadNode> _next = new();

    public Vector3 Position => transform.position;

    public RoadNode GetNext()
    {
        if(_next.Count == 0) return null;
        if(_next.Count == 1) return _next[0];

        return _next[Random.Range(0, _next.Count)];
    }

    public RoadNode GetNext(Vector3 travelDirection)
    {
        if(_next.Count <= 1) return GetNext();

        travelDirection.y = 0f;
        if(travelDirection.sqrMagnitude < 0.0001f) return GetNext();
        travelDirection.Normalize();

        var forward = new List<RoadNode>(_next.Count);
        foreach(RoadNode node in _next)
        {
            if(node == null) continue;

            Vector3 to = node.Position - transform.position;
            to.y = 0f;
            if(to.sqrMagnitude < 0.0001f) continue;

            // Отсекаем только то, что строго позади: повороты налево и направо остаются
            if(Vector3.Dot(to.normalized, travelDirection) > -0.5f) forward.Add(node);
        }

        // Тупик в конце дороги: развернуться здесь — единственный вариант
        if(forward.Count == 0) return GetNext();

        return forward[Random.Range(0, forward.Count)];
    }
}
