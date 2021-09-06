using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class GerenciadorQueda : MonoBehaviour
{
    enum ExecucaoFisica : byte { Nenhuma, CPU, GPU };

    public ComputeShader computeShader;
    public GameObject cuboPrefab;

    public int quantiaObjetos = 100;
    public float minimoMassa = 0.1f;
    public float maximaMassa = 100;
    public float minimoVelocidade = 0.1f;
    public float maximaVelocidade = 100;

    public float forca = 2f;

    //Fisica[] resultado = new Fisica[] { new Fisica() {massa = 5, velocidade = 10 } }; // <- do teste do Laport
    ExecucaoFisica execucaoFisica;

    GameObject[] gameObjects;
    Fisica[] fisicas;

    struct Fisica
    {
        public float massa;
        public float velocidade;

        public Vector3 posicao;
    };

    
    // Update is called once per frame
    void Update()
    {
        switch (execucaoFisica)
        {
            case ExecucaoFisica.CPU:
                CpuAtualiza(); break;
            case ExecucaoFisica.GPU:
                GpuAtualiza(); break;
        }

        /*if (fisicaGo == null)
            return;

        testeFisica.velocidade += (forca / testeFisica.massa) * Time.deltaTime;
        testeFisica.posicao.y -= testeFisica.velocidade * Time.deltaTime;

        fisicaGo.transform.position = testeFisica.posicao;*/
    }

    void CpuAtualiza()
    {
        for (int i = 0; i < gameObjects.Length; i++)
        {
            fisicas[i].velocidade += (forca / fisicas[i].massa) * Time.deltaTime;
            fisicas[i].posicao.y -= fisicas[i].velocidade * Time.deltaTime;

            gameObjects[i].transform.position = fisicas[i].posicao;
        }
    }

    void GpuAtualiza()
    {
        computeShader.SetFloat("deltaT", Time.deltaTime);
        computeShader.SetFloat("forca", forca);

        ComputeBuffer computeBuffer = new ComputeBuffer(fisicas.Length, (sizeof(float) * 2) + (sizeof(float) * 3));
        computeBuffer.SetData(fisicas);

        computeShader.SetBuffer(0, "fisicas", computeBuffer);
        computeShader.Dispatch(0, fisicas.Length / 10, 1, 1);

        computeBuffer.GetData(fisicas);

        for (int i = 0; i < gameObjects.Length; i++)
        {
            gameObjects[i].transform.position = fisicas[i].posicao;
        }

        computeBuffer.Dispose();
    }

    private void OnGUI()
    {
        if (fisicas == null)
        {
            if (GUI.Button(new Rect(0, 0, 100, 50), "Começar"))
            {
                BotaoComecar();
            }
        }
        else
        {
            if (GUI.Button(new Rect(120, 0, 100, 50), "Física CPU"))
            {
                BotaoFisicaCpu();
            }
            else if (GUI.Button(new Rect(220, 0, 100, 50), "Física GPU"))
            {
                BotaoFisicaGpu();
            }
        }
    }

    void TentaResetaPosicoes()
    {
        if (execucaoFisica == ExecucaoFisica.Nenhuma)
            return;

        int _quantia = fisicas.Length;
        for (int i = 0; i < _quantia; i++)
        {
            Vector3 _posicao = new Vector3((-_quantia / 2f) + i, 9f, 0f);
            fisicas[i].posicao = _posicao;
            gameObjects[i].transform.position = _posicao;
        }
    }

    void BotaoComecar()
    {
        gameObjects = new GameObject[quantiaObjetos];
        fisicas = new Fisica[quantiaObjetos];

        for (int i = 0; i < quantiaObjetos; i++)
        {
            Vector3 _posicao = new Vector3((-quantiaObjetos / 2f) + i, 9f, 0f);

            gameObjects[i] = Instantiate(cuboPrefab, _posicao, Quaternion.identity);
            gameObjects[i].name = "Cubo #" + i;

            fisicas[i] = new Fisica()
            {
                massa = Random.Range(minimoMassa, maximaMassa),
                velocidade = Random.Range(minimoVelocidade, maximaVelocidade),
                posicao = _posicao
            };
        }
    }

    void BotaoFisicaCpu()
    {
        TentaResetaPosicoes();
        execucaoFisica = ExecucaoFisica.CPU;
    }

    void BotaoFisicaGpu()
    {
        TentaResetaPosicoes();
        execucaoFisica = ExecucaoFisica.GPU;
    }

    /*private void BotaoTeste()
    {
        computeShader.SetFloats("minimoMaximoMassa", new float[] { minimoMassa, maximaMassa });
        computeShader.SetFloats("minimoMaximoVelocidade", new float[] { minimoVelocidade, maximaVelocidade });
        ComputeBuffer computeBuffer = new ComputeBuffer(1, sizeof(float) * 3);
        computeShader.SetBuffer(0, "fisicas", computeBuffer);
        computeBuffer.SetData(resultado);
        computeShader.Dispatch(0, 1 , 1, 1);
        computeBuffer.GetData(resultado);

        for (int i = 0; i < resultado.Length; i++)
        {
            print(resultado[i].massa +"=" + resultado[i].velocidade);

        }
        computeBuffer.Dispose();
    }*/

    /*private void BotaoTesteFormula()
    {
        if (fisicaGo == null)
            fisicaGo = Instantiate(cuboPrefab);
        fisicaGo.transform.position = Vector3.up * 10f;
        BotaoTeste();
        testeFisica = resultado[0];

        testeFisica.posicao = fisicaGo.transform.position;
    }*/
}
