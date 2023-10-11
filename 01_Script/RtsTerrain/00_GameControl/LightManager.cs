using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class LightManager : MonoBehaviour
{
    public LightType _type;
    
    public CSM_Action csm_action;
    public CBM_Action cbm_action;

    public GameObject panel_lightTransform;
    public GameObject panel_lightType;

    Slider slider_ax;
    Slider slider_ay;
    Slider slider_az;

    InputField input_ax;
    InputField input_ay;
    InputField input_az;

    Toggle toggle_l0;
    Toggle toggle_l1;
    Toggle toggle_l2;

    public static ROBuffer<LightData> light_Buffer { get; set; }
    public static LightData[] light_data { get; set; }

    public new Light light;
    public static LightType type { get; set; }
    public static LightManager instance { get; set; }


    public void Init()
    {
        {
            instance = this;
        }

        {
            light = GetComponent<Light>();            
        }

        {
            light_Buffer = new ROBuffer<LightData>(1);
            light_data = light_Buffer.data;
        }

        {
            csm_action.Init();
            cbm_action.Init();
        }

        {
            //StartCoroutine(UpdateTransform_Spot());

            //routineSpot = UpdateTransform_Spot();
            //routinePoint = UpdateTransform_Point();
        }

        {
            routine = new IEnumerator[3];
            routine[0] = UpdateTransform_Spot();
            routine[1] = UpdateTransform_Direct();
            routine[2] = UpdateTransform_Point();
        }

        {
            Init_UIcontrol();
        }
    }

    public void Enable()
    {
        
    }

    public void Disable()
    {

    }

    public void Begin()
    {

    }

    void Init_UIcontrol()
    {
        {
            var slider = panel_lightTransform.GetComponentsInChildren<Slider>();
            var input_field = panel_lightTransform.GetComponentsInChildren<InputField>();
            var toggle = panel_lightType.GetComponentsInChildren<Toggle>();

            slider_ax = slider[0];
            slider_ay = slider[1];
            slider_az = slider[2];

            input_ax = input_field[0];
            input_ay = input_field[1];
            input_az = input_field[2];

            toggle_l0 = toggle[0];
            toggle_l1 = toggle[1];
            toggle_l2 = toggle[2];
        }

        {
            slider_ax.minValue = 0.0f;
            slider_ax.maxValue = 90.0f;

            slider_ay.minValue = 0.0f;
            slider_ay.maxValue = 360.0f;

            slider_az.minValue = 0.0f;
            slider_az.maxValue = 150.0f;

            slider_ax.onValueChanged.AddListener(
                (value) =>
                {
                    lightTransform.x = value;
                    input_ax.text = value.ToString();
                });

            slider_ay.onValueChanged.AddListener(
                (value) =>
                {
                    lightTransform.y = value;
                    input_ay.text = value.ToString();
                });

            slider_az.onValueChanged.AddListener(
                (value) =>
                {
                    lightTransform.z = value;
                    input_az.text = value.ToString();
                });

            slider_ax.value = lightTransform.x;
            slider_ay.value = lightTransform.y;
            slider_az.value = lightTransform.z;
        }

        {
            input_ax.onEndEdit.AddListener(
                (text) =>
                {
                    float value = 0;
                    if(float.TryParse(text, out value))
                    {
                        var slider = slider_ax;
                        value = math.clamp(value, slider.minValue, slider.maxValue);
                        slider.value = value;
                    }
                });

            input_ay.onEndEdit.AddListener(
                (text) =>
                {
                    float value = 0;
                    if (float.TryParse(text, out value))
                    {
                        var slider = slider_ay;
                        value = math.clamp(value, slider.minValue, slider.maxValue);
                        slider.value = value;
                    }
                });

            input_az.onEndEdit.AddListener(
                (text) =>
                {
                    float value = 0;
                    if (float.TryParse(text, out value))
                    {
                        var slider = slider_az;
                        value = math.clamp(value, slider.minValue, slider.maxValue);
                        slider.value = value;
                    }
                });
        }

        {
            toggle_l0.onValueChanged.AddListener(
                (value) =>
                {
                    if(value) { _type = LightType.Spot; }
                });

            toggle_l1.onValueChanged.AddListener(
                (value) =>
                {
                    if (value) { _type = LightType.Directional; }
                });

            toggle_l2.onValueChanged.AddListener(
                (value) =>
                {
                    if (value) { _type = LightType.Point; }
                });

            switch(_type)
            {
                case LightType.Spot:
                {
                    toggle_l0.isOn = true;
                    break;
                }
                case LightType.Directional:
                {
                    toggle_l1.isOn = true;
                    break;
                }
                case LightType.Point:
                {
                    toggle_l2.isOn = true;
                    break;
                }
            }
        }
        
    }

   
    //public float3 lightTransform = new float3(45.0f, 45.0f, 30.0f);  //x: ax,  y : ay,  z : r
    //public float3 lightTransform = new float3(45.0f, 45.0f, 0.0f);
    //public float3 lightTransform = new float3(45.0f, 45.0f, 30.0f);  //x: ax,  y : ay,  z : r

    public float3 lightTransform = new float3(45.0f, 45.0f, 30.0f);

    IEnumerator routineSpot;
    IEnumerator routinePoint;
    IEnumerator[] routine;

    IEnumerator UpdateTransform_Spot()
    {
        Camera cam = Camera.main;
        Transform trMainCam = Camera.main.transform;

        float3 center = float3.zero;

        float preFarPlane = cbm_action._fis.w;

        while (true)
        {
            if(type == LightType.Spot)
            {
                quaternion rot0;
                float3 pos0;
                float ax = lightTransform.x;
                float ay = lightTransform.y;
                float radius = lightTransform.z;

               
                float3 xaxis;
                float3 yaxis;
                float3 zaxis;

                //{
                //    Ray ray = new Ray(trMainCam.position, math.rotate(trMainCam.rotation, new float3(0.0f, 0.0f, 1.0f)));
                //    RaycastHit hit;
                //    if (Physics.Raycast(ray, out hit, 500.0f, LayerMask.GetMask("Terrain")))
                //    {
                //        center = hit.point;
                //
                //        float r = math.distance(trMainCam.position, center);                  
                //        
                //        radius = r < lightTransform.z ? r : lightTransform.z;
                //        //if (lightTransform.z > r)
                //        //{
                //        //    radius = r;
                //        //}
                //        //else
                //        //{
                //        //    radius = lightTransform.z;
                //        //}
                //    }                    
                //}

                {
                    Ray ray = new Ray(trMainCam.position, math.rotate(trMainCam.rotation, new float3(0.0f, 0.0f, 1.0f)));
                    float3 n = new float3(0.0f, -1.0f, 0.0f);
                    float3 p0 = new float3(0.0f, 0.0f, 0.0f);
                
                    float3 d = math.normalize(ray.direction);
                    float3 l0 = ray.origin;
                
                    float k0 = math.dot(n, d);
                    float k1 = math.dot(n, (p0 - l0));
                
                    float t = 0.0f;
                    t = (math.abs(k0) < 0.1f) ? radius : (k1 / k0);
                
                    float3 l1 = l0 + t * d;
                    float r = math.distance(l1, trMainCam.position);
                    center = l1;

                    radius = r < lightTransform.z ? r : lightTransform.z;
                }


                {
                    xaxis = math.rotate(trMainCam.rotation, new float3(1.0f, 0.0f, 0.0f));
                    yaxis = new float3(0.0f, 1.0f, 0.0f);
                    zaxis = math.cross(xaxis, yaxis);

                    rot0 = new quaternion(new float3x3(xaxis, yaxis, zaxis));
                }

                {
                    rot0 = math.mul(math.mul(
                        rot0,
                        quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(ay))),
                        quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), math.radians(ax)));
                }

                {
                    zaxis = math.rotate(rot0, new float3(0.0f, 0.0f, 1.0f));
                    pos0 = center - radius * zaxis;
                }

                transform.position = pos0;
                transform.rotation = rot0;

                {
                    float csmFarPalne = csm_action.fi[3].w;
                    cbm_action._fis.w = csmFarPalne < preFarPlane ? csmFarPalne : preFarPlane;
                }
            }
                     
            yield return null;
        }
    }

    IEnumerator UpdateTransform_Direct()
    {
        Camera cam = Camera.main;
        Transform trMainCam = cam.transform;

        float3 center = float3.zero;

        float preFarPlane = cbm_action._fis.w;

        while (true)
        {
            if (type == LightType.Directional)
            {
                quaternion rot0;
                float3 pos0;
                float ax = lightTransform.x;
                float ay = lightTransform.y;

                {
                    rot0 = math.mul(
                            quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(ay)),
                            quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), math.radians(ax)));

                }

                transform.rotation = rot0;
            }

            yield return null;
        }
    }

    IEnumerator UpdateTransform_Point()
    {
        Camera cam = Camera.main;
        Transform trMainCam = cam.transform;

        float3 center = float3.zero;
      
        float preFarPlane = cbm_action._fis.w;

        while (true)
        {
            if (type == LightType.Point)
            {
                quaternion rot0;
                float3 pos0;
                float ax =      lightTransform.x;
                float ay =      lightTransform.y;
                float radius =  lightTransform.z;


                float3 xaxis;
                float3 yaxis;
                float3 zaxis;

                //{
                //    Ray ray = new Ray(trMainCam.position, math.rotate(trMainCam.rotation, new float3(0.0f, 0.0f, 1.0f)));
                //    RaycastHit hit;
                //    if (Physics.Raycast(ray, out hit, 500.0f, LayerMask.GetMask("Terrain")))
                //    {
                //        center = hit.point;
                //
                //        float r = math.distance(trMainCam.position, center);                  
                //        
                //        radius = r < lightTransform.z ? r : lightTransform.z;
                //        //if (lightTransform.z > r)
                //        //{
                //        //    radius = r;
                //        //}
                //        //else
                //        //{
                //        //    radius = lightTransform.z;
                //        //}
                //    }                    
                //}

                {
                    Ray ray = new Ray(trMainCam.position, math.rotate(trMainCam.rotation, new float3(0.0f, 0.0f, 1.0f)));
                    float3 n = new float3(0.0f, -1.0f, 0.0f);
                    float3 p0 = new float3(0.0f, 0.0f, 0.0f);

                    float3 d = math.normalize(ray.direction);
                    float3 l0 = ray.origin;

                    float k0 = math.dot(n, d);
                    float k1 = math.dot(n, (p0 - l0));

                    float t = 0.0f;
                    t = (math.abs(k0) < 0.1f) ? radius : (k1 / k0);

                    float3 l1 = l0 + t * d;
                    float r = math.distance(l1, trMainCam.position);
                    center = l1;

                    radius = r < lightTransform.z ? r : lightTransform.z;                    
                }

                {
                    xaxis = math.rotate(trMainCam.rotation, new float3(1.0f, 0.0f, 0.0f));
                    yaxis = new float3(0.0f, 1.0f, 0.0f);
                    zaxis = math.cross(xaxis, yaxis);

                    rot0 = new quaternion(new float3x3(xaxis, yaxis, zaxis));
                }

                {
                    rot0 = math.mul(math.mul(
                        rot0,
                        quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(ay))),
                        quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), math.radians(ax)));
                }

                {
                    zaxis = math.rotate(rot0, new float3(0.0f, 0.0f, 1.0f));
                    pos0 = center - radius * zaxis;
                }

                transform.position = pos0;
                transform.rotation = rot0;
                //transform.rotation = quaternion.identity;


                {
                    float csmFarPalne = csm_action.fi[3].w;
                    cbm_action._fis.w = csmFarPalne < preFarPlane ? csmFarPalne : preFarPlane;
                }
            }

            yield return null;
        }
    }


    public static float3 posW;
    public static float3 dirW;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.bUpdate)
        {
            return;
        }

        {
            type = light.type = _type;
        }

        {
            //UpdateSlider();

            for(int i = 0; i < 3; i++)
            {
                routine[i].MoveNext();
            }
        }

        {
            csm_action.UpdateCSM();
            cbm_action.UpdateCBM();
        }

        {
            posW = transform.position;
            dirW = math.rotate(transform.rotation, new float3(0.0f, 0.0f, 1.0f));
        }

        if(light_Buffer != null)
        {
            var data = light_Buffer.data;
            
            data[0].type = (int)(light.type);
            data[0].range = light.range;
            data[0].intensityFactor = light.intensity;
            data[0].color = (Vector4)(light.color);            
            data[0].spotAngle_half = math.radians(light.spotAngle * 0.5f);

            data[0].far_plane = cbm_action._fis.w;
            data[0].specularPow = 1.0f;
               
            data[0].posW = posW;
            data[0].dirW = dirW;

            light_Buffer.Write();
        }
                
    }

    private void OnDestroy()
    {
        BufferBase<LightData>.Release(light_Buffer);
    }
}

public struct LightData
{
    public float intensityFactor;
    public float3 posW;
    
    public float specularPow;
    public float3 dirW;
    
    public float4 color;
    
    public int type;
    
    //for Cube            
    public float far_plane;
    public float range;
    
    //for Spot        
    public float spotAngle_half;
}
