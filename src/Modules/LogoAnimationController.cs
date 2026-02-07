using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using UnityEngine;
using Random = UnityEngine.Random;
using static TONX.Modules.BallInfo;

namespace TONX.Modules;

/// <summary>
/// Logo动画控制器
/// </summary>
public class LogoAnimationController : MonoBehaviour
{
    public float logoPopDuration = 1.5f;
    public float ballSize = 0.8f;
    
    public Color ballColor = Main.ModColor32;
    public Sprite ballSprite;
    public float[] flyOutStaggerDelays = [0f, 0.3f, 0.6f];
    
    public Vector3[] flyOutTargets = new Vector3[3];
    
    private SpriteRenderer logoRenderer;
    private readonly List<Vector3> originalScales = [];
    private Camera mainCamera;
    private Vector3 screenCenter;
    
    // 防止微小延迟
    private bool allBallsArrived;
    private int ballsArrivedCount;
    
    public void Initialize(SpriteRenderer sprite, Camera camera = null)
    {
        logoRenderer = sprite;
        mainCamera = camera ?? Camera.main;
    
        // 防止出现什么莫名其妙的问题
        if (mainCamera != null)
        {
            screenCenter = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
            screenCenter.z = 0;
        
            var screenBottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
            var screenTopRight = mainCamera.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));
        
            var screenWidth = screenTopRight.x - screenBottomLeft.x;
            var screenHeight = screenTopRight.y - screenBottomLeft.y;
        
            var minDistance = Mathf.Min(screenWidth, screenHeight) * 0.25f;
            var maxDistance = Mathf.Min(screenWidth, screenHeight) * 0.4f;
        
            GenerateRandomPositions(minDistance, maxDistance);
        }
        else
        {
            screenCenter = Vector3.zero;
        }
    }

    private void GenerateRandomPositions(float minDistance, float maxDistance)
    {
        while (true)
        {
            // 防止三个点在同一象限
            var angles = new float[3];
            angles[0] = Random.Range(30f, 90f) * Mathf.Deg2Rad;
            angles[1] = Random.Range(150f, 210f) * Mathf.Deg2Rad;
            angles[2] = Random.Range(270f, 330f) * Mathf.Deg2Rad;

            for (var i = 0; i < 3; i++)
            {
                angles[i] += Random.Range(-15f, 15f) * Mathf.Deg2Rad;
                var distance = Random.Range(minDistance, maxDistance);
                flyOutTargets[i] = new Vector3(Mathf.Cos(angles[i]) * distance, Mathf.Sin(angles[i]) * distance, 0);
            }

            var positiveXCount = 0;
            var positiveYCount = 0;

            foreach (var pos in flyOutTargets)
            {
                if (pos.x > 0) positiveXCount++;
                if (pos.y > 0) positiveYCount++;
            }

            if (positiveXCount == 0 || positiveXCount == 3 || positiveYCount == 0 || positiveYCount == 3)
            {
                continue;
            }

            break;
        }
    }

    public IEnumerator PlayAnimationSequence()
    {

        logoRenderer.enabled = false;
        logoRenderer.sortingOrder = 1;
        logoRenderer.sortingLayerName = "UI";
        
        allBallsArrived = false;
        ballsArrivedCount = 0;
        yield return new WaitForSeconds(2);
        yield return CreateAndPositionBalls();
        yield return StartCoroutine(BallsLightUp().WrapToIl2Cpp());
        while (!allBallsArrived)
        {
            yield return null;
        }
        
        StartCoroutine(CreateGlowCircle().WrapToIl2Cpp());
        yield return StartCoroutine(LogoElegantAppear().WrapToIl2Cpp());
        StartCoroutine(LogoBreatheEffect().WrapToIl2Cpp());
        CleanupBalls();
    }
    
    private IEnumerator CreateAndPositionBalls()
    {
        BallInfos.Clear();
        originalScales.Clear();
        
        for (var i = 0; i < 3; i++)
        {
            var ballObj = new GameObject($"LogoBall_{i}")
            {
                transform =
                {
                    position = screenCenter + flyOutTargets[i]
                }
            };

            var renderer = ballObj.AddComponent<SpriteRenderer>();
            
            var ballColorVariation = new Color(
                ballColor.r * (0.9f + i * 0.05f),
                ballColor.g,
                ballColor.b * (1.1f - i * 0.05f),
                0f
            );
            renderer.color = ballColorVariation;
            
            if (ballSprite != null)
            {
                renderer.sprite = ballSprite;
            }
            else
            {
                var tex = CreateGlowingBallTexture(64, 64, ballColorVariation);
                var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                renderer.sprite = sprite;
            }
            
            var scale = Vector3.one * ballSize;
            ballObj.transform.localScale = scale;
            
            Create(ballObj, ballObj.transform.position);
            originalScales.Add(scale);
        }
        
        yield return null;
    }
    


private IEnumerator BallsLightUp()
{
    for (var i = 0; i < BallInfos.Count; i++)
    {
        if (BallInfos[i] == null) continue;
        
        var ballInfo = BallInfos[i];
        var ball = ballInfo.BallObject;
        var renderer = ball.GetComponent<SpriteRenderer>();

        var delay = flyOutStaggerDelays[i];
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }
        
        StartCoroutine(MoveBallToCenter(ballInfo, i).WrapToIl2Cpp());
        
        var elapsed = 0f;
        const float lightUpDuration = 0.5f;
        
        var startColor = renderer.color;
        var targetColor = new Color(startColor.r, startColor.g, startColor.b, 1f);
        var startScale = ball.transform.localScale;
        var targetScale = startScale * 1.3f;
        
        while (elapsed < lightUpDuration)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / lightUpDuration;
            
            var easedT = EaseOutSine(t);
            renderer.color = Color.Lerp(startColor, targetColor, easedT);
            
            // 缩放
            float scaleT;
            if (t < 0.5f)
            {
                scaleT = t * 2f;
                ball.transform.localScale = Vector3.Lerp(startScale, targetScale, EaseOutSine(scaleT));
            }
            else
            {
                scaleT = (t - 0.5f) * 2f;
                ball.transform.localScale = Vector3.Lerp(targetScale, startScale, EaseInSine(scaleT));
            }
            
            yield return null;
        }
        
        renderer.color = targetColor;
        ball.transform.localScale = startScale;
    }
}
private bool accelerationPhaseStarted;
private IEnumerator MoveBallToCenter(BallInfo ballInfo, int index)
{
    var ball = ballInfo.BallObject;
    var startPosition = ballInfo.StartPosition;
    var controlPoint = ballInfo.ControlPoint;
    var targetPosition = ballInfo.TargetPosition;
    
    const float initialSpeed = 1.3f;
    var currentSpeed = initialSpeed;
    var acceleration = 0f;

    var totalPathLength = ballInfo.TotalDistance();
    var currentT = 0f;
    
    var timer = 0f;
    var originalScale = originalScales[index];
    const float rotationSpeed = 180f;
    
    while (currentT < 1f)
    {
        timer += Time.deltaTime;
        
        if (index == 2 && timer >= 0.7f && !accelerationPhaseStarted)
        {
            accelerationPhaseStarted = true;
        }
        
        var traveledLength = AdaptiveBezierLength(startPosition, controlPoint, targetPosition, 0f, currentT, 0.001f);
        var remainingLength = totalPathLength - traveledLength;
        
        switch (accelerationPhaseStarted)
        {
            case false:
            {
                const float remainingTimeBeforeAcceleration = 1.7f;
                
                // 根据剩余路程和剩余时间计算所需加速度
                // 使用加速度推导公式 a = 2*(s - v0*t)/t^2
                acceleration = 2f * (remainingLength - currentSpeed * remainingTimeBeforeAcceleration) / 
                              (remainingTimeBeforeAcceleration * remainingTimeBeforeAcceleration);
                
                // 确保加速度为正（加速运动）
                acceleration = Mathf.Max(acceleration, 0.1f);
                break;
            }
            case true when acceleration > 0:
                currentSpeed += acceleration * Time.deltaTime;
                break;
        }
        
        // 计算这一帧应该移动的曲线长度
        var moveLength = currentSpeed * Time.deltaTime;
        
        if (moveLength > remainingLength)
        {
            currentT = 1f;
        }
        else
        {
            var deltaT = CalculateDeltaTForLength(startPosition, controlPoint, targetPosition, 
                currentT, moveLength, totalPathLength);
            currentT += deltaT;
            currentT = Mathf.Clamp01(currentT);
        }
        
        var newPosition = CalculateQuadraticBezier(startPosition, controlPoint, targetPosition, currentT);
        ball.transform.position = newPosition;
        
        var tangent = CalculateBezierTangent(startPosition, controlPoint, targetPosition, currentT);
        if (tangent.magnitude > 0.01f)
        {
            var angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
            ball.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        
        ball.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        var scaleMultiplier = Mathf.Lerp(1f, 0.3f, currentT);
        ball.transform.localScale = originalScale * scaleMultiplier;
        
        yield return null;
    }
    
    if (ball == null) yield break;
    ball.transform.position = targetPosition;
    ball.transform.localScale = originalScale * 0.3f;
    CheckAllBallsArrived();
}


private void CheckAllBallsArrived()
    {
        ballsArrivedCount++;
        
        if (ballsArrivedCount >= BallInfos.Count && !allBallsArrived)
        {
            allBallsArrived = true;
        }
    }
    
    private IEnumerator CreateGlowCircle()
    {
        var glowCircle = new GameObject("GlowCircle")
        {
            transform =
            {
                position = screenCenter,
                localScale = Vector3.zero
            }
        };

        var circleRenderer = glowCircle.AddComponent<SpriteRenderer>();
        var circleColor = new Color(1f, 0.4f, 0.7f, 0f);
        
        var circleTex = CreateIntenseGlowTexture(256, 256, circleColor);
        var circleSprite = Sprite.Create(circleTex, 
            new Rect(0, 0, circleTex.width, circleTex.height), 
            new Vector2(0.5f, 0.5f));
        circleRenderer.sprite = circleSprite;
        circleRenderer.sortingOrder = 0;
        circleRenderer.sortingLayerName = "UI";
        

        var elapsed = 0f;
        const float intensifyDuration = 0.5f;
        
        while (elapsed < intensifyDuration)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / intensifyDuration;

            var scale = Mathf.Lerp(0f, 3f, EaseOutBack(t));
            var alpha = Mathf.Lerp(0f, 1f, t);
            
            glowCircle.transform.localScale = Vector3.one * scale;
            circleRenderer.color = new Color(circleColor.r, circleColor.g, circleColor.b, alpha);
            
            foreach (var ballInfo in BallInfos)
            {
                var ball = ballInfo?.BallObject;
                if (ball == null) continue;
                
                var renderer = ball.GetComponent<SpriteRenderer>();
                if (renderer == null) continue;
                var ballAlpha = Mathf.Lerp(1f, 0f, t);
                renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, ballAlpha);
            }
            
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        elapsed = 0f;
        const float fadeOutDuration = 1.0f;
        var startFadeColor = circleRenderer.color;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / fadeOutDuration;
            
            var alpha = Mathf.Lerp(startFadeColor.a, 0f, EaseInSine(t));
            circleRenderer.color = new Color(startFadeColor.r, startFadeColor.g, startFadeColor.b, alpha);
            
            var scale = Mathf.Lerp(3f, 0.5f, t);
            glowCircle.transform.localScale = Vector3.one * scale;
            
            yield return null;
        }
        
        Destroy(glowCircle);
    }
    
    private IEnumerator LogoElegantAppear()
    {
        if (logoRenderer == null) yield break;
        
        logoRenderer.enabled = true;
        logoRenderer.color = new Color(1, 1, 1, 0);
        
        var logoTransform = logoRenderer.transform;
        var originalScale = logoTransform.localScale;
        
        logoTransform.localScale = originalScale * 0.8f;
        
        var elapsed = 0f;
        while (elapsed < logoPopDuration)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / logoPopDuration;
            
            var scaleT = EaseOutSine(t);
            var fadeT = EaseInOutSine(t);
            
            var scale = Mathf.Lerp(0.8f, 1f, scaleT);
            logoTransform.localScale = originalScale * scale;
            
            logoRenderer.color = new Color(1, 1, 1, fadeT);
            
            if (t < 0.7f)
            {
                var glow = Mathf.Sin(t * Mathf.PI * 2f) * 0.1f + 1f;
                logoRenderer.color = new Color(glow, glow, glow, fadeT);
            }
            
            yield return null;
        }
        
        logoTransform.localScale = originalScale;
        logoRenderer.color = Color.white;
    }
    
    private IEnumerator LogoBreatheEffect()
    {
        if (logoRenderer == null) yield break;
    
        var logoTransform = logoRenderer.transform;
        var originalScale = logoTransform.localScale;
        var timer = 0f;
    
        while (true)
        {
            if (logoRenderer == null) yield break;
        
            timer += Time.deltaTime * 1.5f;
            var breathe = Mathf.Sin(timer) * 0.02f + 1f;
            logoTransform.localScale = originalScale * breathe;
        
            yield return null;
        }
    }
    
    private static Texture2D CreateGlowingBallTexture(int width, int height, Color baseColor)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var centerColor = Color.white;
        var edgeColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        
        var center = new Vector2(width * 0.5f, height * 0.5f);
        var maxRadius = Mathf.Min(width, height) * 0.4f;
        
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixelPos = new Vector2(x, y);
                var distance = Vector2.Distance(pixelPos, center);
                var normalizedDistance = distance / maxRadius;
                
                if (normalizedDistance <= 1f)
                {
                    var intensity = 1f - Mathf.Pow(normalizedDistance, 1.5f);
                    var pixelColor = Color.Lerp(edgeColor, centerColor, intensity);
                    
                    if (normalizedDistance > 0.7f)
                    {
                        var halo = (1f - normalizedDistance) / 0.3f;
                        pixelColor.a *= halo;
                    }
                    
                    tex.SetPixel(x, y, pixelColor);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        tex.Apply();
        return tex;
    }
    
    private static Texture2D CreateIntenseGlowTexture(int width, int height, Color baseColor)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        var center = new Vector2(width * 0.5f, height * 0.5f);
        var maxRadius = Mathf.Min(width, height) * 0.5f;
        
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixelPos = new Vector2(x, y);
                var distance = Vector2.Distance(pixelPos, center);
                var normalizedDistance = distance / maxRadius;
                
                if (normalizedDistance <= 1f)
                {
                    var intensity = 1f - Mathf.Pow(normalizedDistance, 1.2f);
                    
                    var pixelColor = normalizedDistance < 0.3f ? 
                        Color.Lerp(Color.white, baseColor, normalizedDistance / 0.3f) : baseColor;
                    
                    pixelColor.a = intensity * 0.9f;
                    tex.SetPixel(x, y, pixelColor);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        tex.Apply();
        return tex;
    }
    
    private void CleanupBalls()
    {
        foreach (var ball in BallInfos.Where(ball => ball != null))
        {
            Destroy(ball.BallObject);
        }
        
        BallInfos.Clear();
        originalScales.Clear();
    }
    
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
    }
    /*弃用
    private static float EaseInCubic(float t)
    {
        return t * t * t;
    }
    
    private static float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
    }
    */
    private static float EaseOutSine(float t)
    {
        return Mathf.Sin(t * Mathf.PI * 0.5f);
    }
    
    private static float EaseInSine(float t)
    {
        return 1 - Mathf.Cos(t * Mathf.PI * 0.5f);
    }
    
    private static float EaseInOutSine(float t)
    {
        return -(Mathf.Cos(Mathf.PI * t) - 1) / 2;
    }
}

// 这东西之后可以修改一下，这样别的地方也能用上
/// <summary>
/// BallInfo用于存储每个球的信息
/// </summary>
/// <param name="ballObj">小球</param>
/// <param name="startPos">起点</param>
public class BallInfo(GameObject ballObj, Vector3 startPos)
{
    public static readonly List<BallInfo> BallInfos = [];
    internal static void Create(GameObject ballObj, Vector3 startPos)
    {
        var ball = new BallInfo(ballObj, startPos);
        BallInfos.Add(ball);
    }

    private static Vector3 ScreenCenter => Camera.main!.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
    public GameObject BallObject => ballObj;

    public Vector3 StartPosition => startPos;

    public Vector3 ControlPoint
    {
        get
        {
            var toCenter = ScreenCenter - StartPosition;
            var direction = toCenter.normalized;
            var perpendicular = new Vector3(-direction.y, direction.x, 0);

            const float curveDirection = -1f;
            const float curveStrength = 7.7f;

           return Vector3.Lerp(StartPosition, ScreenCenter, 0.5f) + perpendicular * curveDirection * curveStrength;
        }
    }

    public Vector3 TargetPosition => ScreenCenter;
    
    public float TotalDistance() => CalculateBezierLength(StartPosition, ControlPoint, TargetPosition); 

    private static float CalculateBezierLength(Vector3 p0, Vector3 p1, Vector3 p2, float tolerance = 0.001f)
    {
        return AdaptiveBezierLength(p0, p1, p2, 0f, 1f, tolerance);
    }

    public static float AdaptiveBezierLength(Vector3 p0, Vector3 p1, Vector3 p2, float t0, float t1, float tolerance)
    {
        var start = CalculateQuadraticBezier(p0, p1, p2, t0);
        var end = CalculateQuadraticBezier(p0, p1, p2, t1);

        var midT = (t0 + t1) * 0.5f;
        var midPoint = CalculateQuadraticBezier(p0, p1, p2, midT);

        var straightDistance = Vector3.Distance(start, end);
        var segmentedDistance = Vector3.Distance(start, midPoint) + 
                                Vector3.Distance(midPoint, end);

        if (Mathf.Abs(straightDistance - segmentedDistance) < tolerance)
        {
            return segmentedDistance;
        }

        var leftLength = AdaptiveBezierLength(p0, p1, p2, t0, midT, tolerance);
        var rightLength = AdaptiveBezierLength(p0, p1, p2, midT, t1, tolerance);
    
        return leftLength + rightLength;
    }
    
    public static Vector3 CalculateQuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        t = Mathf.Clamp01(t);
        var u = 1 - t;
        
        var point = u * u * p0;
        point += 2 * u * t * p1;
        point += t * t * p2;
        return point;
    }

    public static Vector3 CalculateBezierTangent(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        // 二次贝塞尔曲线的导数（切线）
        // B'(t) = 2(1-t)(p1-p0) + 2t(p2-p1)
        t = Mathf.Clamp01(t);
    
        var tangent = 2 * (1 - t) * (p1 - p0) + 2 * t * (p2 - p1);
        return tangent;
    }

    public static float CalculateDeltaTForLength(Vector3 p0, Vector3 p1, Vector3 p2,
        float currentT, float targetLength, float totalLength, int iterations = 3)
    {
        var t = currentT;
        var deltaT = targetLength / totalLength;
        for (var i = 0; i < iterations; i++)
        {
            // 计算当前t点的切线长度（速度）
            var tangent = CalculateBezierTangent(p0, p1, p2, t);
            var speedAtT = tangent.magnitude;

            if (speedAtT < 0.0001f) break;

            // 使用导数调整deltaT
            deltaT = targetLength / speedAtT;
            t = Mathf.Clamp01(currentT + deltaT);
        }

        return deltaT;
    }
}