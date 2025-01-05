using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeCell : MonoBehaviour
{
    [SerializeField] public GameObject _leftWall;
    [SerializeField] public GameObject _rightWall;
    [SerializeField] public GameObject _frontWall;
    [SerializeField] public GameObject _backWall;
    [SerializeField] public GameObject _unvisitedBlock;

    public bool IsVisited { get; private set; }

    public void Visit()
    {
        IsVisited = true;
        if (_unvisitedBlock != null)
        {
            _unvisitedBlock.SetActive(false);
        }
    }

    public void ClearLeftWall()
    {
        if (_leftWall != null) _leftWall.SetActive(false);
    }

    public void ClearRightWall()
    {
        if (_rightWall != null) _rightWall.SetActive(false);
    }

    public void ClearFrontWall()
    {
        if (_frontWall != null) _frontWall.SetActive(false);
    }

    public void ClearBackWall()
    {
        if (_backWall != null) _backWall.SetActive(false);
    }

    public void SetLeftWall(bool active)
    {
        if (_leftWall != null) _leftWall.SetActive(active);
    }

    public void SetRightWall(bool active)
    {
        if (_rightWall != null) _rightWall.SetActive(active);
    }

    public void SetFrontWall(bool active)
    {
        if (_frontWall != null) _frontWall.SetActive(active);
    }

    public void SetBackWall(bool active)
    {
        if (_backWall != null) _backWall.SetActive(active);
    }

    public void ClearAllWalls()
    {
        Visit();
        ClearLeftWall();
        ClearRightWall();
        ClearFrontWall();
        ClearBackWall();
    }

    public void SetAllWalls(bool active)
    {
        SetLeftWall(active);
        SetRightWall(active);
        SetFrontWall(active);
        SetBackWall(active);
    }
}
