namespace SilenceInCity;

public class SilenceMono : MonoBehaviour
{
    public static List<SilenceMono> all = new();
    public CircleProjector m_projector;

    private void Awake()
    {
        all.Add(this);
        m_projector ??= GetComponent<CircleProjector>();
    }

    private void OnDestroy() { all.Remove(this); }

    public static bool InsideArea(Vector3 point)
    {
        foreach (var x in all)
            if (x.IsInside(point, 0.0f))
                return true;

        return false;
    }

    public bool IsInside(Vector3 point, float radius)
    {
        return transform.DistanceXZ(point) < m_projector.m_radius + radius;
    }
}