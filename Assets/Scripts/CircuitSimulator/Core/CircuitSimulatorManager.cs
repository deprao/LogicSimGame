using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;


/// <summary>
/// Esse script é responsável pela instanciação dos módulos do CircuitSimulator.
/// Os parâmetros devem ser instanciados aqui e passados para os objetos através do constructor ou de setters
/// </summary>
public class CircuitSimulatorManager : Singleton<CircuitSimulatorManager>
{
    [Header("Player Dependecies")]
    public GameObject circuitSimulatorPlayer;
    public PlayerInput circuitSimulatorPlayerInput;
    public SpriteRenderer circuitSimulatorPlayerCursor;
    public Sprite defaultCursorSprite;
    public CircuitSimulatorPlacingCircuits circuitSimulatorPlacingCircuits;

    [Space(10)]
    [Header("Manager Settings")]
    public Grid grid;
    public int backgroundWidth;
    public int backgroundHeight;
    public GameObject logicGatesParent;

    // Armazena os Gates que existem na cena para deleção e outras coisas.
    [HideInInspector] public List<Gate> gates = new List<Gate>();

    /// <summary>
    /// Módulo de gerenciamento do circuitSimulator
    /// </summary>
    [HideInInspector] public CircuitSimulatorRenderer circuitSimulatorRenderer;

    [Space(10)]
    [Header("Logic Gates Prefabs")]
    public GameObject Generator;
    public GameObject AND;
    public GameObject NAND;
    public GameObject OR;
    public GameObject NOR;
    public GameObject XOR;
    public GameObject XNOR;
    public GameObject NOT;
    public GameObject Reader;

    [HideInInspector] public List<GameObject> levelGates = new List<GameObject>();
    [HideInInspector] public List<GameObject> userGates = new List<GameObject>();

    void Start()
    {
        circuitSimulatorRenderer = GetComponent<CircuitSimulatorRenderer>();
        circuitSimulatorRenderer.width = backgroundWidth;
        circuitSimulatorRenderer.height = backgroundHeight;
        circuitSimulatorRenderer.FillBackground();
        
        //TODO: Adicionar todos os gates do level na lista

        var g1 = Instantiate(Generator, logicGatesParent.transform);
        g1.GetComponent<GENERATORGate>().Initialize(new Vector3Int(3, 11));
        g1.GetComponent<GENERATORGate>().SetState(true);
        levelGates.Add(g1);
        
        var g2 = Instantiate(Generator, logicGatesParent.transform);
        g2.GetComponent<GENERATORGate>().Initialize(new Vector3Int(3, 9));
        g2.GetComponent<GENERATORGate>().SetState(true);
        levelGates.Add(g2);
        
        var g3 = Instantiate(Generator, logicGatesParent.transform);
        g3.GetComponent<GENERATORGate>().Initialize(new Vector3Int(3, 7));
        g3.GetComponent<GENERATORGate>().SetState(true);
        levelGates.Add(g3);
        
        var and = Instantiate(AND, logicGatesParent.transform);
        and.GetComponent<ANDGate>().Initialize(new Vector3Int(8, 10));
        and.GetComponent<ANDGate>().ChangeInput1(g1);
        and.GetComponent<ANDGate>().ChangeInput2(g2);
        levelGates.Add(and);
        
        var not1 = Instantiate(NOT, logicGatesParent.transform);
        not1.GetComponent<NOTGate>().Initialize(new Vector3Int(5, 7));
        not1.GetComponent<NOTGate>().ChangeInput1(g3);
        levelGates.Add(not1);
        
        var not2 = Instantiate(NOT, logicGatesParent.transform);
        not2.GetComponent<NOTGate>().Initialize(new Vector3Int(7, 8));
        not2.GetComponent<NOTGate>().ChangeInput1(g2);
        levelGates.Add(not2);

        var nor = Instantiate(NOR, logicGatesParent.transform);
        nor.GetComponent<NORGate>().Initialize(new Vector3Int(10, 7));
        nor.GetComponent<NORGate>().ChangeInput1(not2);
        nor.GetComponent<NORGate>().ChangeInput2(not1);
        levelGates.Add(nor);
        
        var xor = Instantiate(XOR, logicGatesParent.transform);
        xor.GetComponent<XORGate>().Initialize(new Vector3Int(14, 9));
        xor.GetComponent<XORGate>().ChangeInput1(and);
        xor.GetComponent<XORGate>().ChangeInput2(nor);
        levelGates.Add(xor);

        var reader = Instantiate(Reader, logicGatesParent.transform);
        reader.GetComponent<READERGate>().Initialize(new Vector3Int(18, 9));
        reader.GetComponent<READERGate>().ChangeInput1(xor);
        levelGates.Add(reader);

        GameManager.Instance.ChangeState(GameState.CircuitSimulatorMoving);
        circuitSimulatorPlayerInput.SwitchCurrentActionMap("Movement");
        SoundManager.Instance.PlayMusic(0);
        SoundManager.Instance.ChangeMusicVolume(0.015f);
        
        StartCoroutine(changeGenerators(g1, g2, g3));

    }

    public void StartPlacingWires()
    {
        // Pegar posição em célula
        // Iterar pra ver se algum output está nesse local
        // Se não tem, faz barulho de erro e retorna
        // Se tem, altera o CurrentActionMap
        // Executa CircuitSimulatorPlacingWires.Setup()
        CircuitSimulatorManager.Instance.circuitSimulatorPlayerInput.SwitchCurrentActionMap("PlacingWires");
        
    }

    public void StartPlacingCircuit(InventoryItem item)
    {
        GameObject gate = ConvertItemDataToPrefab(item);
        if (gate == null) return;
        
        circuitSimulatorPlacingCircuits.Setup(gate, item);
        UpdateCursorSprite(gate);
        circuitSimulatorPlacingCircuits.canPlace = circuitSimulatorPlacingCircuits.CheckPosition(circuitSimulatorPlacingCircuits.cellPosition);
        UpdateCursorSpriteAvailability(circuitSimulatorPlacingCircuits.canPlace);
        
        GameManager.Instance.ChangeState(GameState.CircuitSimulatorPlacingCircuits);
        
    }

    public void UpdateCursorSprite(GameObject gateGO)
    {
        Gate gate = gateGO.GetComponent<Gate>();
        circuitSimulatorPlayerCursor.sprite = gate.properties.lightSprite;
        circuitSimulatorPlayerCursor.color = new Color(1f, 1f, 1f, 0.6f);
    }

    public void UpdateCursorSpriteAvailability(bool isValid)
    {
        circuitSimulatorPlayerCursor.color =
            isValid ? ColorPalette.validPlacePosition : ColorPalette.invalidPlacePosition;
    }
    
    public void ResetCursorSprite()
    {
        circuitSimulatorPlayerCursor.sprite = defaultCursorSprite;
        circuitSimulatorPlayerCursor.color = new Color(1f, 1f, 1f, 1f);
    }
    
    public GameObject ConvertItemDataToPrefab(InventoryItem item)
    {
        switch (item.data.id)
        {
            case "andItem":
                return AND;
            case "nandItem":
                return NAND;
            case "orItem":
                return OR;
            case "norItem":
                return NOR;
            case "xorItem":
                return XOR;
            case "xnorItem":
                return XNOR;
            case "notItem":
                return NOT;
        }

        return null;
    }

    // // Itera sobre a lista de Gates que foram inseridas pelo o jogador e deleta todos
    // // Deleta os fios também. É um soft reset da cena.
    // public void DeleteAllGates()
    // {
    //     foreach (var gate in gates.ToList())
    //     {
    //         foreach (var wire in gate.outputWires.ToList())
    //         {
    //             wire.RemoveLineRenderer();
    //             Destroy(wire);
    //         }
    //         Destroy(gate);
    //     }
    //     circuitSimulatorRenderer.logicGatesTileMap.ClearAllTiles();
    // }

    public IEnumerator changeGenerators(GameObject go1, GameObject go2, GameObject go3)
    {
        yield return new WaitForSeconds(1.5f);
        go1.GetComponent<GENERATORGate>().SetState(!go1.GetComponent<GENERATORGate>().output.state);
        yield return new WaitForSeconds(1.5f);
        go2.GetComponent<GENERATORGate>().SetState(!go2.GetComponent<GENERATORGate>().output.state);
        yield return new WaitForSeconds(1.5f);
        go3.GetComponent<GENERATORGate>().SetState(!go3.GetComponent<GENERATORGate>().output.state);
        yield return new WaitForSeconds(1.5f);
        go1.GetComponent<GENERATORGate>().SetState(!go1.GetComponent<GENERATORGate>().output.state);
        yield return new WaitForSeconds(1.5f);
        go2.GetComponent<GENERATORGate>().SetState(!go2.GetComponent<GENERATORGate>().output.state);
        yield return new WaitForSeconds(1.5f);
        go3.GetComponent<GENERATORGate>().SetState(!go3.GetComponent<GENERATORGate>().output.state);
        StartCoroutine(changeGenerators(go1, go2, go3));
    }
    
}