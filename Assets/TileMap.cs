using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEditor;

public class TileMap : MonoBehaviour {

	/**
	 * TileSet representation
	 */
	public class TileSet {
		public int firstgid;
		public string name;
		public int tileWidth;
		public int tileHeight;
		public string imageSource;
		public TileSet(int firstgid, string name, int tileWidth, int tileHeight, string imageSource){
			this.firstgid = firstgid;
			this.name = name;
			this.tileWidth = tileWidth;
			this.tileHeight = tileHeight;
			this.imageSource = imageSource;
		}
	}

	public static string ASSETS_DIR = "Assets";
	public static string TILEDMAP_FOLDER = ASSETS_DIR + "/Resources";
	public static string TILEDMAP_PREF_DIR = TILEDMAP_FOLDER + "/Pref/";
	public static string TILEDMAP_IMAGE_DIR = TILEDMAP_FOLDER + "/Sprits/";
	public static string PREFAB_RESOURCE_DIR = "Pref/";
	
	public static TileMap tileMap = null;

	private GameObject mapObject = null;
	private string tileMapPath = null;
	private Dictionary<string, TileSet> tileGrid = null;
	private int tileWidth;
	private int tileHeight;

	/**
	 * The function called by tool bar.
	 */
	[MenuItem("TileMap/LoadToScene")]
	public static void openTiledMap() {
		createFolder ();
		string pathToMapFile = EditorUtility.OpenFilePanel(
			"Tiled Map File",
			"",
			"tmx");
		if (Path.GetExtension(pathToMapFile) != ".tmx") {
			Debug.LogWarning("Not tmx: file extension" + Path.GetExtension(pathToMapFile));
			return;
		}
		
		Undo.RegisterSceneUndo("Create Empty Local");
		tileMap = new TileMap();
		GameObject mapObject = new GameObject();
		mapObject.transform.position = Vector3.zero;
		mapObject.transform.rotation = Quaternion.identity;
		mapObject.transform.localScale = new Vector3(1.0f,1.0f,1.0f);
		mapObject.name = "TileMap";
		tileMap.setMapObject (mapObject);

		Selection.activeTransform = tileMap.mapObject.transform;
		loadTiledMap (pathToMapFile);

		Debug.Log ("Done");
	}

	/**
	 * Load xml file to generate tile Dictionary
	 */
	private static void loadTiledMap(string mapFilePath) {
		XmlDocument xmlTiledMap = new XmlDocument ();
		xmlTiledMap.Load (mapFilePath);
		tileMap.setTileMapPaht(mapFilePath);
		Dictionary<string, TileSet> tileGrid = new Dictionary<string, TileSet>();
		XmlNodeList tilesetList = xmlTiledMap.GetElementsByTagName ("tileset");	
		foreach (XmlNode node in tilesetList ) {
			tileGrid.Add(node.Attributes["firstgid"].Value, 
			             new TileSet(
							int.Parse(node.Attributes["firstgid"].Value),
							node.Attributes["name"].Value, 
							int.Parse(node.Attributes["tilewidth"].Value), 
							int.Parse(node.Attributes["tileheight"].Value),
				            generatePrefab(node["image"].Attributes["source"].Value))
			             );
		}
		tileMap.setTileGrid (tileGrid);
		XmlNodeList mapList = xmlTiledMap.GetElementsByTagName ("map");
		foreach (XmlNode node in mapList) {
			tileMap.setTileWidth(int.Parse(node.Attributes["tilewidth"].Value)).setTileHeight(int.Parse(node.Attributes["tileheight"].Value));
		}
		XmlNodeList layers = xmlTiledMap.GetElementsByTagName ("layer");
		foreach(XmlNode layer in layers) {
			processLayerData(layer);
		}
	}

	/**
	 * Generate Prefab. If prefab exists, skip generating.
	 */
	private static string generatePrefab(string imageFile) {
		string spriteName = Path.GetFileNameWithoutExtension (imageFile);

		if (Resources.Load(PREFAB_RESOURCE_DIR + spriteName ) == null ) {
			string pref = TILEDMAP_PREF_DIR + spriteName + ".prefab";
			Object prefab = EditorUtility.CreateEmptyPrefab(pref);
			Sprite sprite = Resources.Load <Sprite> (spriteName);
			GameObject go = new GameObject("ToCreatePrefab");
			SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
			renderer.sprite = sprite;
			EditorUtility.ReplacePrefab(go, prefab);
			DestroyImmediate (go);
			AssetDatabase.Refresh();
		}
		return spriteName;
	}

	/**
	 * Process layer and put prefab into scene.
	 */
	private static void processLayerData(XmlNode layer) {
		XmlNodeList tiles = layer["data"].SelectNodes ("tile");
		int width = int.Parse (layer.Attributes ["width"].Value);
		int height = int.Parse(layer.Attributes ["height"].Value);
		GameObject layerObject = new GameObject();
		layerObject.transform.parent = tileMap.mapObject.transform;
		layerObject.name = layer.Attributes ["name"].Value;
		layerObject.transform.localScale = new Vector3 (1, 1, 0);
		layerObject.transform.localPosition = Vector3.zero;
		int tileCount = 0;
 		foreach (XmlNode tileNode in tiles) {
			if (tileNode.Attributes["gid"].Value != "0" && tileMap.tileGrid.ContainsKey(tileNode.Attributes["gid"].Value)) {
				TileSet tileSet = tileMap.tileGrid[tileNode.Attributes["gid"].Value];
				GameObject tile = Instantiate(Resources.Load(PREFAB_RESOURCE_DIR + tileSet.imageSource, typeof(GameObject))) as GameObject;
				int x = tileCount % width;
				int y = height - 1 - tileCount / width;
				tile.transform.parent = layerObject.transform;
				tile.transform.localScale = new Vector3 (tileSet.tileWidth / tileMap.getTileWidth(), tileSet.tileHeight / tileMap.getTileHeight(), 0);
				tile.name = tileMap.tileGrid[tileNode.Attributes["gid"].Value].name;
				tile.transform.localPosition = new Vector3(x, y, 0);
			}
			tileCount ++;
		}
	}

	/**
	 * Give access to private element.
	 */
	private static void createFolder(){
		Directory.CreateDirectory (TILEDMAP_FOLDER);
		Directory.CreateDirectory (TILEDMAP_PREF_DIR);
		Directory.CreateDirectory (TILEDMAP_IMAGE_DIR);
	}

	public TileMap setTileMapPaht(string path){
		tileMapPath = path;
		return this;
	}
	
	public TileMap setTileGrid(Dictionary<string, TileSet> grid) {
		tileGrid = grid;
		return this;
	}
	
	public TileMap setTileWidth(int width) {
		tileWidth = width;
		return this;
	}
	
	public int getTileWidth(){
		return tileWidth;
	}
	
	public int getTileHeight() {
		return tileHeight;
	}
	
	public TileMap setTileHeight(int height){
		tileHeight = height;
		return this;
	}
	
	public TileMap setMapObject (GameObject go) {
		mapObject = go;
		return this;
	}
	
	public GameObject getMapObject(){
		return mapObject;
	}
}