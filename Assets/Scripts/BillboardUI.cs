using System;
using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    [SerializeField] private Vector3 _rotation = new (90, 0, 0);
    private void LateUpdate() {
        transform.rotation = Quaternion.Euler(_rotation);
    }
}
