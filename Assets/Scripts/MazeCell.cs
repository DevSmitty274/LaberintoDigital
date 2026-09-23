using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MazeCell : MonoBehaviour
{
    [SerializeField]
    private GameObject _leftWall;

    [SerializeField]
    private GameObject _rightWall;

    [SerializeField]
    private GameObject _frontWall;

    [SerializeField]
    private GameObject _backWall;

    [SerializeField]
    private GameObject _unvisitedBlock;

    public bool IsVisited { get; private set; }

    // --- Getters usados por el solver (BFS) del MazeGenerator ---
    public bool HasLeftWall => _leftWall.activeSelf;
    public bool HasRightWall => _rightWall.activeSelf;
    public bool HasFrontWall => _frontWall.activeSelf;
    public bool HasBackWall => _backWall.activeSelf;

    public void Visit()
    {
        IsVisited = true;
        _unvisitedBlock.SetActive(false);
    }

    public void ClearLeftWall()
    {
        _leftWall.SetActive(false);
    }

    public void ClearRightWall()
    {
        _rightWall.SetActive(false);
    }

    public void ClearFrontWall()
    {
        _frontWall.SetActive(false);
    }

    public void ClearBackWall()
    {
        _backWall.SetActive(false);
    }
}
