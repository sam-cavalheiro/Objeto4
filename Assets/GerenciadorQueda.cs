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
    Transform plano;

    GameObject[] gameObjects = new GameObject[0];
    Fisica[] fisicas = new Fisica[0];

    [System.Serializable]
    struct Fisica
    {
        public int indiceGameObject;

        public float massa;
        public float velocidade;
        public Vector3 posicao;
        public Color cor;
    };

    private void Start()
    {
        plano = GameObject.Find("Plano").transform;
    }

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
        int repousoQuantia = 0;

        for (int i = 0; i < fisicas.Length; i++)
        {
            fisicas[i].velocidade += (forca / fisicas[i].massa) * Time.deltaTime;
            fisicas[i].posicao.y -= fisicas[i].velocidade * Time.deltaTime;

            int indiceGo = fisicas[i].indiceGameObject;
            Vector3 _posicao = fisicas[i].posicao;
            gameObjects[indiceGo].transform.position = _posicao;
            if (_posicao.y <= plano.position.y + 0.5f) // 0.5f é a metade da altura do cubo
            {
                gameObjects[indiceGo].GetComponent<MeshRenderer>().material.SetColor("_Color", Random.ColorHSV());
                fisicas[i].indiceGameObject = -fisicas[i].indiceGameObject - 1;
                repousoQuantia++;
            }
        }

        LimpaRepousadosDaFisica(repousoQuantia);
    }

    void GpuAtualiza()
    {
        computeShader.SetFloat("deltaT", Time.deltaTime);
        computeShader.SetFloat("forca", forca);

        ComputeBuffer computeBuffer = new ComputeBuffer(fisicas.Length, (sizeof(float) * 2) + (sizeof(float) * 3) + (sizeof(float) * 4) /*+ sizeof(bool)*/ + sizeof(int));
        computeBuffer.SetData(fisicas);

        computeShader.SetBuffer(0, "fisicas", computeBuffer);
        computeShader.Dispatch(0, fisicas.Length, 1, 1);

        computeBuffer.GetData(fisicas);

        int repousoQuantia = 0;

        for (int i = 0; i < fisicas.Length; i++)
        {
            Vector3 _posicao = fisicas[i].posicao;
            gameObjects[fisicas[i].indiceGameObject].transform.position = _posicao;
            if (_posicao.y <= plano.position.y + 0.5f) // 0.5f é a metade da altura do cubo
            {
                fisicas[i].indiceGameObject = -fisicas[i].indiceGameObject - 1;
                repousoQuantia++;
            }
        }

        computeBuffer.Dispose();

        Fisica[] emRepouso = LimpaRepousadosDaFisica(repousoQuantia);
        if (emRepouso.Length > 0)
        {
            ComputeBuffer colorCb = new ComputeBuffer(emRepouso.Length, (sizeof(float) * 2) + (sizeof(float) * 3) + (sizeof(float) * 4) /*+ sizeof(bool)*/ + sizeof(int));
            colorCb.SetData(emRepouso);

            computeShader.SetBuffer(1, "fisicas", colorCb);
            computeShader.Dispatch(1, emRepouso.Length, 1, 1);

            colorCb.GetData(emRepouso);
            for (int i = 0; i < emRepouso.Length; i++)
            {
                try
                {
                    gameObjects[-emRepouso[i].indiceGameObject - 1].GetComponent<MeshRenderer>().material.SetColor("_Color", emRepouso[i].cor);
                }
                catch (Exception e)
                {
                    print("Erro: " + (-emRepouso[i].indiceGameObject).ToString());
                }
            }

            colorCb.Dispose();
        }
    }

    Fisica[] LimpaRepousadosDaFisica(int repousoQuantia)
    {
        Fisica[] emRepouso = new Fisica[repousoQuantia];

        if (repousoQuantia > 0)
        {
            int _quantia = fisicas.Length - repousoQuantia;
            Fisica[] _fisicas = new Fisica[_quantia];

            int repousoIndice = 0;
            int j = -1;

            for (int i = 0; i < _quantia; i++)
            {
                while (j < fisicas.Length)
                {
                    j++;
                    if (fisicas[j].indiceGameObject >= 0)
                    {
                        _fisicas[i] = fisicas[j];
                        break;
                    }

                    //emRepouso[repousoIndice++] = fisicas[j];
                }

                if (j >= fisicas.Length)
                    break;
            }
            for (int i = 0; i < fisicas.Length; i++)
            {
                if (fisicas[i].indiceGameObject < 0)
                    emRepouso[repousoIndice++] = fisicas[i];
            }

            fisicas = _fisicas;
            if (fisicas.Length == 0)
                execucaoFisica = ExecucaoFisica.Nenhuma;
        }

        return emRepouso;
    }

    private void OnGUI()
    {
        if (fisicas.Length == 0)
        {
            if (GUI.Button(new Rect(0, 0, 100, 50), "Começar"))
            {
                BotaoComecar();
            }
        }
        else
        {
            if (execucaoFisica == ExecucaoFisica.Nenhuma)
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
            else
            {
                if (GUI.Button(new Rect(0, 0, 100, 50), "Recomeçar"))
                {
                    BotaoRecomecar();
                }
            }
        }
    }

    void BotaoComecar()
    {
        Recomecar();
    }

    void BotaoRecomecar()
    {
        Recomecar();
    }

    void BotaoFisicaCpu()
    {
        execucaoFisica = ExecucaoFisica.CPU;
    }

    void BotaoFisicaGpu()
    {
        execucaoFisica = ExecucaoFisica.GPU;
    }

    void Recomecar()
    {
        // Instanciar novos gameObjects se aumentou a quantia
        if (quantiaObjetos > gameObjects.Length)
        {
            GameObject[] _gameObjects = new GameObject[quantiaObjetos];

            for (int i = 0; i < quantiaObjetos; i++)
            {
                if (i > gameObjects.Length - 1)
                {
                    _gameObjects[i] = Instantiate(cuboPrefab);
                }
                else if (i < gameObjects.Length)
                {
                    _gameObjects[i] = gameObjects[i];
                }
            }

            gameObjects = _gameObjects;
        }
        // Destruir antigos gameObjects se reduziu a quantia
        else if (quantiaObjetos < gameObjects.Length)
        {
            GameObject[] _gameObjects = new GameObject[quantiaObjetos];

            for (int i = 0; i < gameObjects.Length; i++)
            {
                if (i > quantiaObjetos - 1)
                {
                    Destroy(gameObjects[i]);
                }
                else if (i < quantiaObjetos)
                {
                    _gameObjects[i] = gameObjects[i];
                }
            }

            gameObjects = _gameObjects;
        }

        // Instanciar novas fisicas se o tamanho da array for diferente da quantia
        if (fisicas.Length != quantiaObjetos)
        {
            fisicas = new Fisica[quantiaObjetos];
        }

        // Atribuindo valores iniciais aos objetos de física
        for (int i = 0; i < quantiaObjetos; i++)
        {
            Vector3 _posicao = new Vector3((-quantiaObjetos / 2f) + i, 9f, 0f);
            fisicas[i] = new Fisica()
            {
                massa = Random.Range(minimoMassa, maximaMassa),
                velocidade = Random.Range(minimoVelocidade, maximaVelocidade),
                posicao = _posicao,
                indiceGameObject = i,
                cor = Color.red
            };
            gameObjects[i].transform.position = _posicao;
            gameObjects[i].GetComponent<MeshRenderer>().material.SetColor("_Color", fisicas[i].cor);
        }

        // Atualização de física pronta para iniciar
        execucaoFisica = ExecucaoFisica.Nenhuma;
    }
}
