using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// Drives the whole intro sequence from a single time value, so any frame
// can be previewed in the editor by scrubbing "time".
[ExecuteAlways]
public class IntroDirector : MonoBehaviour
{
    public const float Duration = 30f;

    [Header("Scene references")]
    public Camera cam;
    public Transform cabin;
    public Transform door;
    public Transform boat;
    public WaterSurface water;
    public Volume fade;
    public GameObject rodProp;
    public Transform rodHand;
    public Transform rodTip;
    public Transform bobber;
    public LineRenderer line;
    public Renderer[] ripples;
    public Renderer[] wake;

    [Header("Playback")]
    [Range(0f, Duration)] public float time;
    public bool autoPlay = true;
    public bool fades = true;
    [Tooltip("0 = full sequence. 1, 5, 7 or 8 = keep that shot's camera for the whole range.")]
    public int soloShot;

    [Header("Layout")]
    public Vector3 fishingSpot;
    public float boatHeightOffset;
    public float eyeHeight = 1.65f;

    // Measured from the cabin model, relative to the door hinge (right, forward).
    const float FloorY = 12.71f, DockY = 12.62f, DeckY = 1.10f;
    const float MooringRight = -3.75f, MooringForward = 3.5f;
    const float DoorOpenAngle = 105f;
    const float CastRelease = 26.18f, CastLanding = 27.4f, CastDistance = 16f;

    bool playing;
    float endTime = Duration;
    AnimationCurve[] walkPos, walkLook;
    AnimationCurve rodPitch;
    MaterialPropertyBlock block;
    Vector3 lastWaterHit;

    public bool IsPlaying => playing;

    public void Play(float from, float to)
    {
        time = from;
        endTime = to;
        playing = true;
        Evaluate(time);
    }

    void OnEnable() { walkPos = null; }

    void Start()
    {
        if (Application.isPlaying && autoPlay && !playing) Play(0f, Duration);
    }

    void Update()
    {
        if (Application.isPlaying && playing)
        {
            time += Time.deltaTime;
            if (time >= endTime) { time = endTime; playing = false; }
        }
        Evaluate(time);
    }

    Vector3 Right => cabin.right;
    Vector3 Forward => cabin.forward;
    float WaterLevel => water.transform.position.y;

    // Point in the cabin frame: metres to the right / forward of the door hinge, absolute height.
    Vector3 P(float right, float y, float forward)
    {
        Vector3 o = door.position;
        return new Vector3(o.x, y, o.z) + Right * right + Forward * forward;
    }

    float WaterHeight(Vector3 p)
    {
        var search = new WaterSearchParameters
        {
            startPositionWS = lastWaterHit,
            targetPositionWS = p,
            error = 0.01f,
            maxIterations = 8
        };
        if (water.ProjectPointOnWaterSurface(search, out WaterSearchResult hit))
        {
            lastWaterHit = hit.projectedPositionWS;
            return hit.projectedPositionWS.y;
        }
        return WaterLevel;
    }

    static float Ramp(float a, float b, float t) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, t));
    static float Pulse(float a, float b, float c, float d, float t) => Mathf.Min(Ramp(a, b, t), 1f - Ramp(c, d, t));

    int ShotAt(float t)
    {
        if (soloShot != 0) return soloShot;
        if (t < 12f) return 1;
        if (t < 19.5f) return 5;
        if (t < 24f) return 7;
        return 8;
    }

    public void Evaluate(float t)
    {
        if (!cam || !cabin || !door || !boat || !water) return;
        if (walkPos == null) BuildCurves();

        door.localRotation = Quaternion.Euler(0f, DoorOpenAngle * Ramp(0.5f, 3.3f, t), 0f);
        MoveBoat(t);
        Wake(t);

        int shot = ShotAt(t);
        if (rodProp) rodProp.SetActive(shot != 8);
        if (rodHand) rodHand.gameObject.SetActive(shot == 8);

        switch (shot)
        {
            case 1: WalkCamera(t); break;
            case 5: AerialCamera(t); break;
            case 7: ArrivalCamera(t); break;
            default: CastCamera(t); break;
        }
        Cast(t, shot == 8);

        if (fade)
            fade.weight = fades ? Mathf.Max(Pulse(11f, 12f, 12.2f, 13f, t), Pulse(19f, 19.5f, 19.6f, 20.2f, t)) : 0f;
    }

    // ---------- Boat

    const float Departure = 12.6f, Accel = 2.2f, AccelTime = 3.2f;

    float TravelDistance(float t)
    {
        float tau = Mathf.Max(0f, t - Departure);
        if (tau < AccelTime) return 0.5f * Accel * tau * tau;
        return 0.5f * Accel * AccelTime * AccelTime + Accel * AccelTime * (tau - AccelTime);
    }

    float TravelSpeed(float t) => Accel * Mathf.Clamp(t - Departure, 0f, AccelTime);

    // Foam patches left behind by the hull: three per emission step (port, stern, starboard).
    void Wake(float t)
    {
        if (wake == null) return;
        const float step = 0.16f;
        int steps = wake.Length / 3;
        float life = steps * step;
        float newest = Mathf.Floor(t / step) * step;
        block ??= new MaterialPropertyBlock();
        for (int i = 0; i < steps; i++)
        {
            float emitted = newest - i * step;
            float age = t - emitted;
            bool on = t < 19.5f && emitted > Departure;
            float strength = on ? TravelSpeed(emitted) / (Accel * AccelTime) * Mathf.Pow(1f - age / life, 1.5f) : 0f;
            Vector3 stern = P(MooringRight, WaterLevel + 0.07f, MooringForward) + Forward * TravelDistance(emitted);
            for (int side = -1; side <= 1; side++)
            {
                var r = wake[i * 3 + side + 1];
                if (!r) continue;
                r.enabled = strength > 0.01f;
                if (!r.enabled) continue;
                Vector3 pos = side == 0
                    ? stern - Forward * 0.6f
                    : stern + Forward * 6f + Right * (side * (2f + 1.5f * age));
                float size = (side == 0 ? 3.2f + age * 1.1f : 1.6f + age * 0.6f);
                r.transform.position = pos;
                r.transform.localScale = new Vector3(size, 1f, size);
                block.SetColor("_UnlitColor", new Color(1f, 1f, 1f, strength * (side == 0 ? 0.8f : 0.45f)));
                r.SetPropertyBlock(block);
            }
        }
    }

    void MoveBoat(float t)
    {
        Vector3 pos, heading;
        float speedPitch = 0f;
        if (t < 19.5f)
        {
            pos = P(MooringRight, 0f, MooringForward) + Forward * TravelDistance(t);
            heading = Forward;
            speedPitch = -1.5f * Ramp(12.6f, 15f, t);
        }
        else
        {
            pos = fishingSpot;
            heading = Right;
        }
        Quaternion yaw = Quaternion.LookRotation(heading, Vector3.up);
        pos.y = WaterHeight(pos + heading * 7f) + boatHeightOffset;
        float pitch = 0.7f * Mathf.Sin(t * 0.9f) + speedPitch;
        float roll = 1.1f * Mathf.Sin(t * 0.7f + 1f);
        boat.SetPositionAndRotation(pos, yaw * Quaternion.Euler(pitch, 0f, roll));
    }

    // ---------- Cameras

    static AnimationCurve[] Track(float[] times, Vector3[] values)
    {
        var curves = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };
        for (int i = 0; i < times.Length; i++)
            for (int axis = 0; axis < 3; axis++)
                curves[axis].AddKey(times[i], values[i][axis]);
        foreach (var c in curves)
            for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 0f);
        return curves;
    }

    static Vector3 Sample(AnimationCurve[] c, float t) => new Vector3(c[0].Evaluate(t), c[1].Evaluate(t), c[2].Evaluate(t));

    void BuildCurves()
    {
        float eyeIn = FloorY + eyeHeight, eyeDock = DockY + eyeHeight;
        var times = new[] { 0f, 4f, 6f, 8f, 9.6f, 12f };
        // (right, height, forward) in the cabin frame
        walkPos = Track(times, new[]
        {
            new Vector3(-0.42f, eyeIn, -2.9f),
            new Vector3(-0.42f, eyeIn, -2.4f),
            new Vector3(-0.38f, eyeDock + 0.04f, 0.9f),
            new Vector3(-0.30f, eyeDock, 3.9f),
            new Vector3(-0.45f, eyeDock + 0.15f, 5.3f),
            new Vector3(-1.25f, eyeDock + 1.0f, 5.7f)
        });
        walkLook = Track(times, new[]
        {
            new Vector3(-0.8f, 13.9f, 7f),
            new Vector3(-1.2f, 13.9f, 8f),
            new Vector3(-2.2f, 14.0f, 9f),
            new Vector3(-3.0f, 14.1f, 9f),
            new Vector3(-3.4f, 14.0f, 6.6f),
            new Vector3(MooringRight, 13.9f, MooringForward + 1.9f)
        });
        rodPitch = new AnimationCurve(
            new Keyframe(24f, 30f, 0f, 0f),
            new Keyframe(25f, 30f, 0f, 0f),
            new Keyframe(25.9f, 115f, 0f, 0f),
            new Keyframe(26.25f, 4f, 0f, 0f),
            new Keyframe(27f, 24f, 0f, 0f),
            new Keyframe(30f, 22f, 0f, 0f));
    }

    void SetCamera(Vector3 pos, Vector3 target)
    {
        cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(target - pos, Vector3.up));
    }

    void WalkCamera(float t)
    {
        Vector3 p = Sample(walkPos, t), l = Sample(walkLook, t);
        // Footstep bob while walking (4 s - 9.6 s)
        float walking = Pulse(4f, 4.6f, 9.2f, 9.9f, t);
        float step = (t - 4f) * 1.9f * Mathf.PI * 2f;
        Vector3 pos = P(p.x, p.y, p.z) + Vector3.up * (0.028f * Mathf.Sin(step) * walking) + Right * (0.02f * Mathf.Sin(step * 0.5f) * walking);
        SetCamera(pos, P(l.x, l.y, l.z));
    }

    void AerialCamera(float t)
    {
        Vector3 b = boat.position;
        Vector3 pos = b - Forward * 21f + Right * 9f;
        pos.y = WaterLevel + 14f + 1.3f * (t - 12f);
        SetCamera(pos, b + Forward * 7f + Vector3.up * 1.5f);
    }

    void ArrivalCamera(float t)
    {
        Vector3 center = fishingSpot + Right * 7f;
        float k = Ramp(19.5f, 24.6f, t);
        Vector3 pos = center - Forward * Mathf.Lerp(42f, 19f, k) + Right * Mathf.Lerp(11f, 3f, k);
        pos.y = WaterLevel + Mathf.Lerp(5f, 2.8f, k);
        SetCamera(pos, new Vector3(center.x, WaterLevel + 2.4f, center.z));
    }

    Vector3 CastTarget()
    {
        Vector3 from = boat.TransformPoint(-1.1f, 0f, 2.2f);
        Vector3 dir = (-boat.right - boat.forward * 0.25f);
        dir.y = 0f;
        Vector3 w = from + dir.normalized * CastDistance;
        w.y = WaterLevel;
        return w;
    }

    void CastCamera(float t)
    {
        Vector3 pos = boat.TransformPoint(-1.1f, DeckY + eyeHeight, 2.2f);
        Vector3 w = CastTarget();
        Vector3 horizon = w + Vector3.up * (pos.y - w.y - 0.4f);
        Vector3 target = Vector3.Lerp(horizon, w + Vector3.up * 0.3f, Ramp(26.4f, 28.2f, t));
        SetCamera(pos, target);
    }

    // ---------- Rod, line and ripples

    void Cast(float t, bool visible)
    {
        if (!rodHand || !rodTip || !bobber || !line) return;
        bobber.gameObject.SetActive(visible);
        line.enabled = visible;
        if (!visible)
        {
            if (ripples != null) foreach (var r in ripples) if (r) r.enabled = false;
            return;
        }

        float pitch = rodPitch.Evaluate(Mathf.Max(t, 24f));
        float lift = Mathf.InverseLerp(30f, 115f, pitch);
        rodHand.localPosition = new Vector3(0.27f, -0.40f + 0.14f * lift, 0.42f - 0.10f * lift);
        rodHand.localRotation = Quaternion.Euler(-pitch, -7f, 0f);

        Vector3 tip = rodTip.position;
        Vector3 w = CastTarget();
        float landedY = WaterHeight(w);
        Vector3 hook;
        float sag;
        if (t < CastRelease)
        {
            float hang = Mathf.Lerp(0.5f, 0.06f, Ramp(25.9f, CastRelease, t));
            hook = tip + Vector3.down * hang;
            sag = 0f;
        }
        else if (t < CastLanding)
        {
            float u = Mathf.InverseLerp(CastRelease, CastLanding, t);
            float eased = 1f - Mathf.Pow(1f - u, 1.3f);
            hook = Vector3.Lerp(tip, new Vector3(w.x, landedY, w.z), eased) + Vector3.up * (3.2f * 4f * eased * (1f - eased));
            sag = 0.2f + 0.9f * u;
        }
        else
        {
            hook = new Vector3(w.x, landedY + 0.02f * Mathf.Sin(t * 5f), w.z);
            sag = 1.1f + 1.4f * Ramp(CastLanding, CastLanding + 1.2f, t);
        }
        bobber.position = hook;

        Vector3 control = (tip + hook) * 0.5f + Vector3.down * sag;
        control.y = Mathf.Max(control.y, landedY + 0.15f);
        int n = line.positionCount;
        for (int i = 0; i < n; i++)
        {
            float u = i / (float)(n - 1);
            line.SetPosition(i, Vector3.Lerp(Vector3.Lerp(tip, control, u), Vector3.Lerp(control, hook, u), u));
        }

        if (ripples == null) return;
        block ??= new MaterialPropertyBlock();
        for (int i = 0; i < ripples.Length; i++)
        {
            var r = ripples[i];
            if (!r) continue;
            const float life = 2.6f;
            float age = t - (CastLanding + i * 0.45f);
            r.enabled = age > 0f && age < life;
            if (!r.enabled) continue;
            float size = 0.3f + age * 1.4f;
            r.transform.position = new Vector3(w.x, landedY + 0.05f, w.z);
            r.transform.localScale = new Vector3(size, 1f, size);
            block.SetColor("_UnlitColor", new Color(1f, 1f, 1f, 0.6f * (1f - age / life)));
            r.SetPropertyBlock(block);
        }
    }
}
