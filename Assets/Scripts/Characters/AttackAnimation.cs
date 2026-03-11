using UnityEngine;
using System.Collections;

public class AttackAnimation : MonoBehaviour
{
    public float lungeDistance = 0.2f;
    public float lungeSpeed = 8f;

    public IEnumerator PlayAttack(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;

        Vector3 direction = (targetPosition - startPosition).normalized;

        Vector3 attackPosition = startPosition + direction * lungeDistance;

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * lungeSpeed;
            transform.position = Vector3.Lerp(startPosition, attackPosition, t);
            yield return null;
        }

        t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * lungeSpeed;
            transform.position = Vector3.Lerp(attackPosition, startPosition, t);
            yield return null;
        }
    }
}