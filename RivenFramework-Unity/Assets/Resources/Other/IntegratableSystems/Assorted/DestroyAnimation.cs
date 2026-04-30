using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAnimation : MonoBehaviour
{
    [SerializeField] GameObject objectToDestroy = null;

    public void OnAnimationEnd ()
    {
        if (objectToDestroy == null)
        {
            objectToDestroy = gameObject;
        }
        Destroy(objectToDestroy);
    }
}
