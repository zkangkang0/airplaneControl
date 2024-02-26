using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using Unity.Sentis;
using UnityEngine.UI;
using UnityEngine.Video;
using Lays = Unity.Sentis.Layers;

public class ObjectDetection : MonoBehaviour
{
    public Camera camera;
    public Canvas canvas;
    private RectTransform canvasRectTransform;

    const string modelName = "yolov8n.sentis";
    //const string modelName = "yolov5m.onnx";
    // .sentis�ļ���.onnx�ļ�������

    // Link the classes.txt here: ����Ƿ���ı�ǩ
    public TextAsset labelsAsset;
    // Link to a bounding box texture here: �����Ŀ����Ŀ�
    public Sprite boxTexture;
    // Link to the font for the labels: �����Ŀ���������
    public Font font;

    private Transform displayLocation;

    //�����ģ��
    private Model model; //model���ڴ洢yolov8ģ��
    private IWorker engine; //IWorker��һ���ӿڣ�����ģ������

    private string[] labels; //�����ʲô��˼������

    //The number of classes in the model
    private const int numClasses = 80;

    const BackendType backend = BackendType.GPUCompute;
    //����ָ��yolov8Ŀ�����㷨�ļ���������
    //BackendType��һ��ö�����ͣ�GPUCompute�����е�һ�����ͣ���ʾʹ��GPU����

    int maxOutputBoxes = 64;
    //����ʲô��˼������

    List<GameObject> boxPool = new List<GameObject>(); //�����box�ļ���

    [SerializeField, Range(0, 1)] float iouThreshold = 0.5f;
    [SerializeField, Range(0, 1)] float scoreThreshold = 0.5f;
    //�������Ƿ�Χ

    //Image size for the model ģ����Ҫ�����ͼ��ߴ�
    private const int imageWidth = 640;
    private const int imageHeight = 640;

    //For using tensor operators: ops��ʲô
    Ops ops;

    //bounding box data, ����Ǳ߽��
    public struct BoundingBox
    {
        public float centerX;
        public float centerY;
        public float width;
        public float height;
        public string label;
    }


    //�����������camera1��target texture
    public RenderTexture renderTexture;


    public string savePath = "RenderTexture.png"; // ������ļ�·��������

    Texture2D inputImage;

    // Start is called before the first frame update
    void Start()
    {
        //camera = FindFirstObjectByType<Camera>();
        //canvas = FindFirstObjectByType<Canvas>();


        canvasRectTransform = canvas.GetComponent<RectTransform>();
        displayLocation = canvas.transform;

        Application.targetFrameRate = 60;
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        //ops����backend�ϸ�Чִ��
        ops = WorkerFactory.CreateOps(backend, null);

        //Parse neural net labels �����������ǩ
        labels = labelsAsset.text.Split('\n');

        LoadModel(); //����ģ��

        //Create engine to run model
        //��������ܺ�����ͣ�������CPU����GPU
        engine = WorkerFactory.CreateWorker(backend, model);

    }

    // Update is called once per frame
    void Update()
    {

        camera.targetTexture = renderTexture;
        camera.Render();
        Graphics.Blit(renderTexture, null as RenderTexture);
        camera.targetTexture = null;
        ExecuteML();

        //SaveRenderTexture();


        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit(); //����Escape���˳�����
        }
    }

    

    public void ExecuteML()
    {
        ClearAnnotations();

        // convert the texture into a tensor
        using var input = TextureConverter.ToTensor(renderTexture, imageWidth, imageHeight, 3);


        engine.Execute(input);//��inputImage���textureת��Ϊtensor,��Ϊģ�͵�����

        //ʹ��PeekOutput����ȡ�ƶϵĽ��
        var boxCoords = engine.PeekOutput("boxCoords") as TensorFloat;
        var NMS = engine.PeekOutput("NMS") as TensorInt;
        var classIDs = engine.PeekOutput("classIDs") as TensorInt;


        using var boxIDs = ops.Slice(NMS, new int[] { 2 }, new int[] { 3 }, new int[] { 1 }, new int[] { 1 });
        using var boxIDsFlat = boxIDs.ShallowReshape(new TensorShape(boxIDs.shape.length)) as TensorInt;
        using var output = ops.Gather(boxCoords, boxIDsFlat, 1);
        using var labelIDs = ops.Gather(classIDs, boxIDsFlat, 2);

        output.MakeReadable();
        labelIDs.MakeReadable();

        //float displayWidth = displayImage.rectTransform.rect.width;
        //float displayHeight = displayImage.rectTransform.rect.height;

        float displayWidth = canvasRectTransform.rect.width;
        float displayHeight = canvasRectTransform.rect.height;
        Debug.Log("displayWidth: " + displayWidth);
        Debug.Log("displayHeight: " + displayHeight);

        float scaleX = displayWidth / imageWidth;
        float scaleY = displayHeight / imageHeight;
        Debug.Log("scaleX: " + scaleX);
        Debug.Log("scaleY: " + scaleY);

        //Draw the bounding boxes
        for (int n = 0; n < output.shape[1]; n++)
        {
            var box = new BoundingBox
            {
                centerX = output[0, n, 0] * scaleX - displayWidth / 2,
                centerY = output[0, n, 1] * scaleY - displayHeight / 2,
                width = output[0, n, 2] * scaleX,
                height = output[0, n, 3] * scaleY,

                //centerX = output[0, n, 0],
                //centerY = output[0, n, 1],
                //width = output[0, n, 2],
                //height = output[0, n, 3],

                label = labels[labelIDs[0, 0, n]],
            };
            Debug.Log("centerX: " + box.centerX);
            Debug.Log("centerY: " + box.centerY);
            Debug.Log("width: " + box.width);
            Debug.Log("height: " + box.height);
            DrawBox(box, n);
        }
    }

    public void DrawBox(BoundingBox box, int id)
    {
        //Create the bounding box graphic or get from pool
        GameObject panel;
        GameObject gameObjectBox;
        if (id < boxPool.Count)
        {
            panel = boxPool[id];
            panel.SetActive(true);
        }
        else
        {
            panel = CreateNewBox(box, UnityEngine.Color.yellow);

        }



        //Set box position
        panel.transform.localPosition = new Vector3(box.centerX, -box.centerY);

        //Set box size
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(box.width, box.height);

        //Set label text
        var label = panel.GetComponentInChildren<Text>();
        label.text = box.label;
    }

    public GameObject CreateNewBox(BoundingBox box, UnityEngine.Color color)
    {
        //Create the box and set image

        var panel = new GameObject("ObjectBox"); //����һ���յ���Ϸ����

        panel.AddComponent<CanvasRenderer>(); //���CanvasRenderer�����������ȾCanvas�ϵ�UIԪ��


        Image img = panel.AddComponent<Image>();
        img.color = color;
        img.sprite = boxTexture;
        //boxTexture���Լ�������spirte
        img.type = Image.Type.Simple;
        panel.transform.SetParent(displayLocation, false);
        //panel�ĸ�����canvas��displayLocation�������canvas��λ��



        //Create the label
        var text = new GameObject("ObjectLabel");
        text.AddComponent<CanvasRenderer>();
        text.transform.SetParent(panel.transform, false);
        Text txt = text.AddComponent<Text>();
        txt.font = font;
        txt.color = color;
        txt.fontSize = 40;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;

        RectTransform rt2 = text.GetComponent<RectTransform>();
        rt2.offsetMin = new Vector2(20, rt2.offsetMin.y);
        rt2.offsetMax = new Vector2(0, rt2.offsetMax.y);
        rt2.offsetMin = new Vector2(rt2.offsetMin.x, 0);
        rt2.offsetMax = new Vector2(rt2.offsetMax.x, 30);
        rt2.anchorMin = new Vector2(0, 0);
        rt2.anchorMax = new Vector2(1, 1);

        boxPool.Add(panel);
        return panel;
    }
    void LoadModel()
    {
        //Load model
        model = ModelLoader.Load(Application.streamingAssetsPath + "/" + modelName);

        //The classes are also stored here in JSON format:
        Debug.Log($"Class names: \n{model.Metadata["names"]}");

        //We need to add some layers to choose the best boxes with the NMSLayer

        //Set constants
        model.AddConstant(new Lays.Constant("0", new int[] { 0 }));
        model.AddConstant(new Lays.Constant("1", new int[] { 1 }));
        model.AddConstant(new Lays.Constant("4", new int[] { 4 }));


        model.AddConstant(new Lays.Constant("classes_plus_4", new int[] { numClasses + 4 }));
        model.AddConstant(new Lays.Constant("maxOutputBoxes", new int[] { maxOutputBoxes }));
        model.AddConstant(new Lays.Constant("iouThreshold", new float[] { iouThreshold }));
        model.AddConstant(new Lays.Constant("scoreThreshold", new float[] { scoreThreshold }));

        //Add layers
        model.AddLayer(new Lays.Slice("boxCoords0", "output0", "0", "4", "1"));
        model.AddLayer(new Lays.Transpose("boxCoords", "boxCoords0", new int[] { 0, 2, 1 }));
        model.AddLayer(new Lays.Slice("scores0", "output0", "4", "classes_plus_4", "1"));
        model.AddLayer(new Lays.ReduceMax("scores", new[] { "scores0", "1" }));
        model.AddLayer(new Lays.ArgMax("classIDs", "scores0", 1));

        model.AddLayer(new Lays.NonMaxSuppression("NMS", "boxCoords", "scores",
            "maxOutputBoxes", "iouThreshold", "scoreThreshold",
            centerPointBox: Lays.CenterPointBox.Center
        ));

        model.outputs.Clear();
        model.AddOutput("boxCoords");
        model.AddOutput("classIDs");
        model.AddOutput("NMS");
    }

    public void ClearAnnotations()
    {
        foreach (var box in boxPool)
        {
            box.SetActive(false);
        }
    }

    private void SaveRenderTexture()
    {
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();

        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(savePath, bytes);

        Debug.Log("Render Texture saved to: " + savePath);

        RenderTexture.active = null;
        Destroy(texture);
    }
}
