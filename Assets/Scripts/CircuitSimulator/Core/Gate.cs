using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;
using UnityEngine.Tilemaps;
using Vector2 = UnityEngine.Vector2;

[Serializable]
public struct Properties
{
    [HideInInspector] public Vector3Int gridPosition;
    public Vector3Int size;
    public Sprite sprite;
    public Sprite lightSprite;

    public Properties(Vector3Int gridPosition, Vector3Int size, Sprite sprite, Sprite lightSprite)
    {
        this.gridPosition = gridPosition;
        this.size = size;
        this.sprite = sprite;
        this.lightSprite = lightSprite;
    }
}

public struct Input
{
    public Vector3Int inputPosition;
    public Gate connectedGate;
    public bool state;
    public bool hasGate;

    public Input(Vector3Int inputPosition, Gate connectedGate, bool state, bool hasGate)
    {
        this.inputPosition = inputPosition;
        this.connectedGate = connectedGate;
        this.state = state;
        this.hasGate = hasGate;
    }
    
}

public struct Output
{
    public Vector3Int outputPosition;
    public List<Tuple<Gate, int>> connectedGates;
    public bool state;
    public List<Wire> wires;
    
    public Output(Vector3Int outputPosition, List<Tuple<Gate, int>> connectedGates, bool state)
    {
        this.outputPosition = outputPosition;
        this.connectedGates = connectedGates;
        this.state = state;
        this.wires = new List<Wire>();
    }
    
}

public abstract class Gate : MonoBehaviour
{

    public Properties properties;
    public Input input1;
    public Input input2;
    public Output output;
    
    public event Action InputRemoved;
    public event Action OutputValueChanged;


    public virtual void Initialize(Vector3Int gridPosition)
    {
        properties.gridPosition = gridPosition;
        input1 = new Input(new Vector3Int(gridPosition.x - 1, gridPosition.y + 1), null, false, false);
        input2 = new Input(new Vector3Int(gridPosition.x - 1, gridPosition.y - 1), null, false, false);
        output = new Output(new Vector3Int(gridPosition.x + 1, gridPosition.y), new List<Tuple<Gate, int>>(), false);

        transform.position = CircuitSimulatorManager.Instance.circuitSimulatorRenderer.logicGatesTileMap.GetCellCenterWorld(gridPosition);

    }
    

    public virtual void ChangeInput1(GameObject gateGO)
    {
        RemoveInput1();
        
        // Pega o componente do gameobject e define o novo input como o output do parametro
        Gate gate = gateGO.GetComponent<Gate>();
        input1.connectedGate = gate;
        input1.state = gate.output.state;
        input1.hasGate = true;
        
        // Adiciona um componente de fio como filho do logic gates passado
        Wire wire = gateGO.AddComponent<Wire>();
        wire.Initialize(this, gateGO, gate.output.outputPosition, input1.inputPosition, gate.output.state);
        gate.output.wires.Add(wire);
        
        // Adiciona esse gate como output no parametro passado
        gate.output.connectedGates.Add(new Tuple<Gate, int>(this, 1));
        gate.OutputValueChanged += OnInputChanged;
        OnInputChanged();
    }
  
    public virtual void RemoveInput1()
    {
        if (!input1.hasGate) return;

        // Remove wire between input and output
        int index = input1.connectedGate.output.wires.FindIndex(wire => wire.gate == this);
        input1.connectedGate.output.wires[index].Delete();
        Destroy(input1.connectedGate.output.wires[index].wireObject);
        Destroy(input1.connectedGate.output.wires[index]);
        input1.connectedGate.output.wires.RemoveAll(wire => wire.gate == this);
            
        // Remove the connection between this gate and the input1 gate
        input1.connectedGate.output.connectedGates.RemoveAll(tuple => tuple.Item1 == this);
        input1.connectedGate.OutputValueChanged -= OnInputChanged;
            
        // Execute cleanup on the gate that got removed
        // Mainly for wire removal
        input1.connectedGate.OnOutputRemoved();
            
        // Reset the input1 fields
        input1.connectedGate = null;
        input1.state = false;
        input1.hasGate = false;

        // Trigger the input removed event
        InputRemoved?.Invoke();

        // Recalculate the output of this gate
        OnInputChanged();
    }
    
    public virtual void ChangeInput2(GameObject gateGO)
    {
        RemoveInput2();
        
        // Pega o componente do gameobject e define o novo input como o output do parametro
        Gate gate = gateGO.GetComponent<Gate>();
        input2.connectedGate = gate;
        input2.state = gate.output.state;
        input2.hasGate = true;
        
        // Adiciona um componente de fio como filho do logic gates passado
        Wire wire = gateGO.AddComponent<Wire>();
        wire.Initialize(this, gateGO, gate.output.outputPosition, input2.inputPosition, gate.output.state);
        gate.output.wires.Add(wire);
        
        // Adiciona esse gate como output no parametro passado
        gate.output.connectedGates.Add(new Tuple<Gate, int>(this, 2));
        gate.OutputValueChanged += OnInputChanged;
        OnInputChanged();
    }
  
    public virtual void RemoveInput2()
    {
        if (!input2.hasGate) return;
        
        // Remove wire between input and output
        int index = input2.connectedGate.output.wires.FindIndex(wire => wire.gate == this);
        input2.connectedGate.output.wires[index].Delete();
        Destroy(input2.connectedGate.output.wires[index].wireObject);
        input2.connectedGate.output.wires.RemoveAll(wire => wire.gate == this);

            
        // Remove the connection between this gate and the input2 gate
        input2.connectedGate.output.connectedGates.RemoveAll(tuple => tuple.Item1 == this);
        input2.connectedGate.OutputValueChanged -= OnInputChanged;
            
        // Execute cleanup on the gate that got removed
        // Mainly for wire removal
        input2.connectedGate.OnOutputRemoved();
            
        // Reset the input2 fields
        input2.connectedGate = null;
        input2.state = false;
        input2.hasGate = false;

        // Trigger the input removed event
        InputRemoved?.Invoke();

        // Recalculate the output of this gate
        OnInputChanged();
    }

    public virtual void RemoveOutputs()
    {
        // Remove this gate from all the outputs that it is connected to
        foreach (Tuple<Gate, int> connectedGate in output.connectedGates.ToList())
        {
            if (connectedGate.Item2 == 1)
            {
                connectedGate.Item1.RemoveInput1();
            }

            if (connectedGate.Item2 == 2)
            {
                connectedGate.Item1.RemoveInput2();
            }
        }
    }

    public virtual void Delete()
    {
        RemoveInput1();
        RemoveInput2();
        RemoveOutputs();
        Destroy(this.gameObject);
    }

    public virtual void OnInputChanged()
    {
        
        // Calcula o novo estado
        output.state = Execute();
        
        // Atualiza nos gates que tem esse como entrada o estado calculado
        foreach (var tupleGateInt in output.connectedGates)
        {
            if (tupleGateInt.Item2 == 1)
            {
                tupleGateInt.Item1.input1.state = output.state;
            }

            if (tupleGateInt.Item2 == 2)
            {
                tupleGateInt.Item1.input2.state = output.state;
            }
        }
        
        // Reflete a cor do estado no wire
        foreach (var wire in output.wires)
        {
            wire.UpdateColor(output.state);
        }
        
        // Invoca metodo para gates que dependem desse
        OutputValueChanged?.Invoke();
        
    }

    public virtual void OnOutputRemoved()
    {
        
    }
    
    public abstract bool Execute();
    
}
