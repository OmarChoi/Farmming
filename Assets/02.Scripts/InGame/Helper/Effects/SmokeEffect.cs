using System.Collections;
using UnityEngine;

public class SmokeEffect : MonoBehaviour
{
    [SerializeField] private float _effectDuration = 3f;
    [SerializeField] private float _fadeOutDuration = 1f;

    public void StartSmokeEffect()
    {
        StartCoroutine(FadeOutAndDestroy(_effectDuration, _fadeOutDuration));
    }

    private IEnumerator FadeOutAndDestroy(float duration, float fadeTime)
    {
        float waitTime = Mathf.Max(0f, duration - fadeTime);
        yield return new WaitForSeconds(waitTime);

        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);

        foreach (var p in particles)
        {
            var emission = p.emission;
            emission.enabled = false;
        }

        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);

            foreach (var p in particles)
            {
                if (p == null) continue;
                var main = p.main;
                Color c = main.startColor.color;
                c.a = alpha;
                main.startColor = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

}
