using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;
using UnityEngine.UI;
using System.IO;


public class u2net : MonoBehaviour
{
    const int CONST_RESOLUTION = 320;

    [SerializeField] ModelAsset m_ModelAsset;
    Model runtimeModel;
    IWorker worker;

    [SerializeField] RenderTexture rgbaRenderTexture;
    [SerializeField] Texture2D m_Texture;
    [SerializeField] Image outputRGBA, outputR;
    TensorFloat inputTensor = null;

    // final output pixel size save here
    private int finalWidht, finalHeight;
    private void Awake()
    {
        runtimeModel = ModelLoader.Load(m_ModelAsset);
        worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, runtimeModel);

    }

    // Sends the image to the neural network model and returns the probability that the image is each particular digit.
    public void RemoveBG()
    {
        finalWidht = m_Texture.width; 
        finalHeight = m_Texture.height;

        m_Texture = ResizeTexture(m_Texture, CONST_RESOLUTION, CONST_RESOLUTION);
        inputTensor?.Dispose();
        inputTensor = TextureConverter.ToTensor(m_Texture, CONST_RESOLUTION, CONST_RESOLUTION, 3);
        worker.Execute(inputTensor);
        TensorFloat outputTensor = worker.PeekOutput() as TensorFloat;
        TextureConverter.RenderToTexture(outputTensor, rgbaRenderTexture);

        MuskImage();
    }

    private Texture2D r; 
    private Texture2D u2netOutput; 
    void MuskImage()
    {
        r = u2netOutput = toTexture2D(rgbaRenderTexture);
        outputR.sprite = Sprite.Create(r, new Rect(0, 0, r.width, r.height), Vector2.one * 0.5f);
        // Ensure both inputImage and u2netOutput are assigned
        if (m_Texture != null && u2netOutput != null)
        {
            // Get the pixels of the images
            Color[] inputPixels = m_Texture.GetPixels();
            Color[] maskPixels = u2netOutput.GetPixels();

            // Apply the red channel of the mask to the alpha channel of the input image
            for (int i = 0; i < inputPixels.Length; i++)
            {
                // Set alpha channel based on the red channel of the mask
                inputPixels[i].a = maskPixels[i].r;
            }


            // Calculate the offset to center the constant pixels in the resized texture
            int xOffset = (finalWidht - CONST_RESOLUTION) / 2;
            int yOffset = (finalHeight - CONST_RESOLUTION) / 2;

            // Create a new texture with the modified pixels
            Texture2D resultTexture = new Texture2D(finalWidht, finalHeight);


            // Set the constant pixels in the middle
            for (int y = 0; y < CONST_RESOLUTION; y++)
            {
                for (int x = 0; x < CONST_RESOLUTION; x++)
                {
                    resultTexture.SetPixel(x + xOffset, y + yOffset, inputPixels[y * CONST_RESOLUTION + x]);
                }
            }


            resultTexture.Apply();

            byte[] pngData = resultTexture.EncodeToPNG();

            // Specify the file path where you want to save the image
            string filePath = Path.Combine(Application.dataPath, "SavedImage.png");

            // Write the PNG data to the file
            File.WriteAllBytes(filePath, pngData);

            Debug.Log("Image saved locally at: " + filePath);

            Sprite resultSprite = Sprite.Create(resultTexture, new Rect(0, 0, finalWidht, finalHeight), Vector2.one * 0.5f);
            outputRGBA.sprite = resultSprite;

        }
        else
        {
            Debug.LogError("Assign inputImage and u2netOutput textures in the Unity Editor.");
        }
    }


    Texture2D toTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(CONST_RESOLUTION, CONST_RESOLUTION, TextureFormat.RGB24, false);
        // ReadPixels looks at the active RenderTexture.
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }

    // Resize a Texture2D to the specified width and height
    private Texture2D ResizeTexture(Texture2D inputTexture, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        Graphics.Blit(inputTexture, rt);
        Texture2D resizedTexture = new Texture2D(newWidth, newHeight);
        RenderTexture.active = rt;
        resizedTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        resizedTexture.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return resizedTexture;
    }

}
