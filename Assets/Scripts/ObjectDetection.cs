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
    // .sentis文件和.onnx文件都可以

    // Link the classes.txt here: 这个是分类的标签
    public TextAsset labelsAsset;
    // Link to a bounding box texture here: 这个是目标检测的框
    public Sprite boxTexture;
    // Link to the font for the labels: 这个是目标检测的字体
    public Font font;

    private Transform displayLocation;

    //这个是模型
    private Model model; //model用于存储yolov8模型
    private IWorker engine; //IWorker是一个接口，用于模型推理

    private string[] labels; //这个是什么意思？？？

    //The number of classes in the model
    private const int numClasses = 80;

    const BackendType backend = BackendType.GPUCompute;
    //用于指定yolov8目标检测算法的计算后端类型
    //BackendType是一个枚举类型，GPUCompute是其中的一个类型，表示使用GPU计算

    int maxOutputBoxes = 64;
    //这是什么意思？？？

    List<GameObject> boxPool = new List<GameObject>(); //这个是box的集合

    [SerializeField, Range(0, 1)] float iouThreshold = 0.5f;
    [SerializeField, Range(0, 1)] float scoreThreshold = 0.5f;
    //这两个是范围

    //Image size for the model 模型需要输入的图像尺寸
    private const int imageWidth = 640;
    private const int imageHeight = 640;

    //For using tensor operators: ops是什么
    Ops ops;

    //bounding box data, 这个是边界框
    public struct BoundingBox
    {
        public float centerX;
        public float centerY;
        public float width;
        public float height;
        public string label;
    }


    //用这个来保存camera1的target texture
    public RenderTexture renderTexture;


    public string savePath = "RenderTexture.png"; // 保存的文件路径和名称

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

        //ops能在backend上高效执行
        ops = WorkerFactory.CreateOps(backend, null);

        //Parse neural net labels 解析神经网络标签
        labels = labelsAsset.text.Split('\n');

        LoadModel(); //加载模型

        //Create engine to run model
        //神经网络接受后端类型，可以是CPU或者GPU
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
            Application.Quit(); //按下Escape，退出程序
        }
    }

    

    public void ExecuteML()
    {
        ClearAnnotations();

        // convert the texture into a tensor
        using var input = TextureConverter.ToTensor(renderTexture, imageWidth, imageHeight, 3);


        engine.Execute(input);//将inputImage这个texture转换为tensor,作为模型的输入

        //使用PeekOutput来获取推断的结果
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

        var panel = new GameObject("ObjectBox"); //创建一个空的游戏对象

        panel.AddComponent<CanvasRenderer>(); //添加CanvasRenderer组件，用于渲染Canvas上的UI元素


        Image img = panel.AddComponent<Image>();
        img.color = color;
        img.sprite = boxTexture;
        //boxTexture是自己制作的spirte
        img.type = Image.Type.Simple;
        panel.transform.SetParent(displayLocation, false);
        //panel的父类是canvas，displayLocation代表的是canvas的位置



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
