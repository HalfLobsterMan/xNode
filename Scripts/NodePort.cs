﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[Serializable]
public class NodePort {
    public enum IO { Input, Output }

    public int ConnectionCount { get { return connections.Count; } }
    /// <summary> Return the first connection </summary>
    public NodePort Connection { get { return connections.Count > 0 ? connections[0].Port : null; } }

    public IO direction { get { return _direction; } }
    /// <summary> Is this port connected to anytihng? </summary>
    public bool IsConnected { get { return connections.Count != 0; } }
    public bool IsInput { get { return direction == IO.Input; } }
    public bool IsOutput { get { return direction == IO.Output; } }

    public string fieldName { get { return _fieldName; } }

    [SerializeField] public Node node;
    [SerializeField] private string _fieldName;
    [SerializeField] public Type type;
    [SerializeField] private List<PortConnection> connections = new List<PortConnection>();
    [SerializeField] private IO _direction;

    public NodePort(FieldInfo fieldInfo) {
        _fieldName = fieldInfo.Name;
        type = fieldInfo.FieldType;

        var attribs = fieldInfo.GetCustomAttributes(false);
        for (int i = 0; i < attribs.Length; i++) {
            if (attribs[i] is Node.InputAttribute) _direction = IO.Input;
            else if (attribs[i] is Node.OutputAttribute) _direction = IO.Output;
        }
    }

    public NodePort(NodePort nodePort, Node node) {
        _fieldName = nodePort._fieldName;
        type = nodePort.type;
        this.node = node;
        _direction = nodePort.direction;
    }

    /// <summary> Checks all connections for invalid references, and removes them. </summary>
    public void VerifyConnections() {
        for (int i = connections.Count - 1; i >= 0; i--) {
            if (connections[i].node != null &&
                !string.IsNullOrEmpty(connections[i].fieldName) &&
                connections[i].node.GetPortByFieldName(connections[i].fieldName) != null)
                continue;
            connections.RemoveAt(i);
        }
    }

    public object GetValue() {
        return node.GetValue(this);
    }

    /// <summary> Connect this <see cref="NodePort"/> to another </summary>
    /// <param name="port">The <see cref="NodePort"/> to connect to</param>
    public void Connect(NodePort port) {
        if (connections == null) connections = new List<PortConnection>();
        if (port == null) { Debug.LogWarning("Cannot connect to null port"); return; }
        if (port == this) { Debug.LogWarning("Attempting to connect port to self."); return; }
        if (IsConnectedTo(port)) { Debug.LogWarning("Port already connected. "); return; }
        if (direction == port.direction) { Debug.LogWarning("Cannot connect two " + (direction == IO.Input ? "input" : "output") + " connections"); return; }
        connections.Add(new PortConnection(port));
        if (port.connections == null) port.connections = new List<PortConnection>();
        port.connections.Add(new PortConnection(this));
        node.OnCreateConnection(this, port);
        port.node.OnCreateConnection(this, port);
    }

    public NodePort GetConnection(int i) {
        //If the connection is broken for some reason, remove it.
        if (connections[i].node == null || string.IsNullOrEmpty(connections[i].fieldName)) {
            connections.RemoveAt(i);
            return null;
        }
        NodePort port = connections[i].node.GetPortByFieldName(connections[i].fieldName);
        if (port == null) {
            connections.RemoveAt(i);
            return null;
        }
        return port;
    }

    public bool IsConnectedTo(NodePort port) {
        for (int i = 0; i < connections.Count; i++) {
            if (connections[i].Port == port) return true;
        }
        return false;
    }

    public void Disconnect(NodePort port) {
        for (int i = connections.Count - 1; i >= 0; i--) {
            //Remove matching ports.
            if (connections[i].Port == port) {
                connections.RemoveAt(i);
            }
        }
        for (int i = 0; i < port.connections.Count; i++) {
            if (port.connections[i].Port == this) {
                port.connections.RemoveAt(i);
            }
        }
    }

    public void ClearConnections() {
        while (connections.Count > 0) {
            Disconnect(connections[0].Port);
        }
    }

    [Serializable]
    public class PortConnection {
        [SerializeField] public string fieldName;
        [SerializeField] public Node node;
        public NodePort Port { get { return port != null ? port : port = GetPort(); } }
        [NonSerialized] private NodePort port;

        public PortConnection(NodePort port) {
            this.port = port;
            node = port.node;
            fieldName = port.fieldName;
        }

        /// <summary> Returns the port that this <see cref="PortConnection"/> points to </summary>
        private NodePort GetPort() {
            if (node == null || string.IsNullOrEmpty(fieldName)) return null;

            for (int i = 0; i < node.OutputCount; i++) {
                if (node.outputs[i].fieldName == fieldName) return node.outputs[i];
            }
            for (int i = 0; i < node.InputCount; i++) {
                if (node.inputs[i].fieldName == fieldName) return node.inputs[i];
            }
            return null;
        }
    }
}