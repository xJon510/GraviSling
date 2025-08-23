using UnityEngine;

public class BalloonBundleEgg : MonoBehaviour, IEasterEgg
{
    private IEasterEgg[] _children;

    public void Activate(EasterEggManager mgr, float lifespan, Camera cam)
    {
        if (_children == null || _children.Length == 0)
            _children = GetComponentsInChildren<IEasterEgg>(includeInactive: true);

        foreach (var child in _children)
        {
            // don’t recursively forward to yourself
            if (child != (IEasterEgg)this)
                child.Activate(mgr, lifespan, cam);
        }
    }
}
