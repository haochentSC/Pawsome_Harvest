using UnityEngine;
using System.Collections;


public class FaceManager : MonoBehaviour
{

    // face smile
    [SerializeField] private GameObject smile1;
    [SerializeField] private GameObject smile2;
    [SerializeField] private GameObject smile3;
    [SerializeField] private GameObject frown;

    // pupils of eyes
    [SerializeField] private Transform pupilLeft;
    [SerializeField] private Transform pupilRight;
    [SerializeField] private Transform eyeLeft;
    [SerializeField] private Transform eyeRight;


    private void SetColor(Transform t, Color c)
    {
        if (t == null) return;

        Renderer r = t.GetComponent<Renderer>();
        if (r != null)
            r.material.color = c;
    }


    private IEnumerator BlinkLoop()
    {
        while (true)
        {
            // Wait 6–7 seconds
            yield return new WaitForSeconds(Random.Range(9f, 10f));

            // --- BLINK START ---
            // Pupils disappear
            pupilLeft.gameObject.SetActive(false);
            pupilRight.gameObject.SetActive(false);

            // Eyes turn black
            SetColor(eyeLeft, Color.gray);
            SetColor(eyeRight, Color.gray);

            // Blink lasts ~0.12 seconds
            yield return new WaitForSeconds(0.3f);

            // --- BLINK END ---
            // Pupils return
            pupilLeft.gameObject.SetActive(true);
            pupilRight.gameObject.SetActive(true);

            // Eyes return to white
            SetColor(eyeLeft, Color.white);
            SetColor(eyeRight, Color.white);
        }
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Eyes start white
        SetColor(eyeLeft,  Color.white);
        SetColor(eyeRight, Color.white);

        // Pupils start black
        SetColor(pupilLeft,  Color.black);
        SetColor(pupilRight, Color.black);

        // Mouth lines start black
        SetColor(smile1.transform, Color.gray);
        SetColor(smile2.transform, Color.gray);
        SetColor(smile3.transform, Color.gray);
        SetColor(frown.transform,  Color.gray);

        StartCoroutine(BlinkLoop());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}