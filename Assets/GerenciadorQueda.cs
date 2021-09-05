using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GerenciadorQueda : MonoBehaviour
{
    public ComputeShader computeShader;

    public float minimoMassa = 0.1f;
    public float maximaMassa = 100;

    public float minimoVelocidade = 0.1f;
    public float maximaVelocidade = 100;

    Fisica[] resultado = new Fisica[] { new Fisica() {massa = 5, velocidade = 10 } };

    struct Fisica
    {
        public float massa;
        public float velocidade;
    };

    // Start is called before the first frame update
    void Start()
    {

    }
    /*void Command_RandomGPU()
    {
        ComputeBuffer computeBuffer = new ComputeBuffer(data.Length, 4 * sizeof(float) + 3 * sizeof(float));
        computeBuffer.SetData(data);

        computeShader.SetBuffer(0, "cubes", computeBuffer);
        computeShader.SetInt("iteraction", iteractions);

        computeShader.Dispatch(0, data.Length / 10, 1, 1);

        computeBuffer.GetData(data);

        for (int i = 0; i < gameObjects.Length; i++)
        {
            gameObjects[i].GetComponent<MeshRenderer>().material.SetColor("_Color", data[i].color);
        }

        computeBuffer.Dispose();
    }*/


// Update is called once per frame
void Update()
    {
        
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(0, 0, 100, 50), "testar"))
        {
            BotaoTeste();
        }
    }

    private void BotaoTeste()
    {
        computeShader.SetFloats("minimoMaximoMassa", new float[] { minimoMassa, maximaMassa });
        computeShader.SetFloats("minimoMaximoVelocidade", new float[] { minimoVelocidade, maximaVelocidade });
        ComputeBuffer computeBuffer = new ComputeBuffer(1, sizeof(float) * 2);
        computeShader.SetBuffer(0, "fisicas", computeBuffer);
        computeBuffer.SetData(resultado);
        computeShader.Dispatch(0, 1 , 1, 1);
        computeBuffer.GetData(resultado);

        for (int i = 0; i < resultado.Length; i++)
        {
            print(resultado[i].massa +"=" + resultado[i].velocidade);

        }
        computeBuffer.Dispose();
    }
}
