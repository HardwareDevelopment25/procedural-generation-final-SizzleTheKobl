using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class CameraMovement : MonoBehaviour
{

    [SerializeField] GameObject m_camera;
    void Initalize() 
    { 
        
    }

    public void SetCameraPosition() 
    {
        float x = GameManager.m_mapX / 2;
        float z = GameManager.m_mapY / 2;
        Vector3 position = new Vector3(x, 50, z);
        m_camera.transform.position = position;
    }

    public void MoveCameraLeft(float axis) 
    { 
        if (axis == 1 && m_camera.transform.position.x < (GameManager.m_mapX-10)) 
        {
            Vector3 position = m_camera.transform.position;
            position.x += 10;
            m_camera.transform.position = position;
        }
        else if (axis == -1 && m_camera.transform.position.x > 10)
        {
            Vector3 position = m_camera.transform.position;
            position.x -= 10;
            m_camera.transform.position = position;
        }
    }

    public void MoveCameraUp(float axis)
    {
        if (axis == 1 && m_camera.transform.position.z < (GameManager.m_mapY - 10))
        {
            Vector3 position = m_camera.transform.position;
            position.z += 10;
            m_camera.transform.position = position;
        }
        else if (axis == -1 && m_camera.transform.position.z > 10)
        {
            Vector3 position = m_camera.transform.position;
            position.z -= 10;
            m_camera.transform.position = position;
        }
    }

    public void ZoomCamera(float axis) 
    {
        if (axis == 1 && m_camera.transform.position.y < 100)
        {
            Vector3 position = m_camera.transform.position;
            position.y += 10;
            m_camera.transform.position = position;
        }
        else if (axis == -1 && m_camera.transform.position.y > 20 )
        {
            Vector3 position = m_camera.transform.position;
            position.y -= 10;
            m_camera.transform.position = position;
        }
    }

}
