using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Security;
using System.Threading;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static UnityEditor.Progress;
using Color = UnityEngine.Color;


public class BoardButtonBehaviors : MonoBehaviour
{
    public GameObject board;
    public GameObject[] tiles;
    public GameObject blackButton;
    public GameObject whiteButton;
    public GameObject[] blackPieces;
    public GameObject[] whitePieces;
    public GameObject[] startTiles;
    public GameObject[] oppStartTiles;
    public GameObject allowedMoveDot;
    public GameObject winScreen;

    int localTileSize = 1080;
    bool whiteStartBot = true;
    bool dotsOnBoard = false;
    bool blocked = false;
    GameObject piece = null;
    string turn = "White";

    bool whiteInCheck = false;
    bool blackInCheck = false;
    bool sameLoopWhite = false;
    bool sameLoopBlack = false;
    bool castle = false;

    List<GameObject> whitePawns = new List<GameObject>();
    List<GameObject> blackPawns = new List<GameObject>();

    Dictionary<string, Dictionary<GameObject, Dictionary<string, string>>> BoardDict = new Dictionary<string, Dictionary<GameObject, Dictionary<string, string>>>();

    Dictionary<string, Func<string, GameObject, bool, bool, List<GameObject>>> moveFunctions = new Dictionary<string, Func<string, GameObject, bool, bool, List<GameObject>>>();

    Dictionary<string, Dictionary<string, GameObject>> tilesDict = new Dictionary<string, Dictionary<string, GameObject>>();

    Dictionary<string, bool> hasMovedDict = new Dictionary<string, bool>();

    Dictionary<string, Dictionary<string, Dictionary<GameObject, List<GameObject>>>> possibleMoveDict = new Dictionary<string, Dictionary<string, Dictionary<GameObject, List<GameObject>>>>();

    void Start()
    {
        winScreen.SetActive(false);

        moveFunctions["Pawn"] = GetPawnMoves;
        moveFunctions["Knight"] = GetKnightMoves;
        moveFunctions["Bishop"] = GetBishopMoves;
        moveFunctions["Rook"] = GetRookMoves;
        moveFunctions["King"] = GetKingMoves;
        moveFunctions["Queen"] = GetQueenMoves;

        hasMovedDict["WhiteRook1"] = false;
        hasMovedDict["WhiteRook2"] = false;
        hasMovedDict["BlackRook1"] = false;
        hasMovedDict["BlackRook2"] = false;
        hasMovedDict["WhiteKing"] = false;
        hasMovedDict["BlackKing"] = false;

        BoardDict["White"] = new Dictionary<GameObject, Dictionary<string, string>>();
        BoardDict["Black"] = new Dictionary<GameObject, Dictionary<string, string>>();

        possibleMoveDict = new Dictionary<string, Dictionary<string, Dictionary<GameObject, List<GameObject>>>>();
        possibleMoveDict["White"] = new Dictionary<string, Dictionary<GameObject, List<GameObject>>>();
        possibleMoveDict["Black"] = new Dictionary<string, Dictionary<GameObject, List<GameObject>>>();

        whiteStartBot = true;
        int i = 0;
        foreach (GameObject tile in startTiles)
        {
            whitePieces[i].transform.SetParent(tile.transform);
            whitePieces[i].transform.position = new Vector2(tile.transform.position.x, tile.transform.position.y);
            i++;
        }
        i = 0;
        foreach (GameObject tile in oppStartTiles)
        {
            blackPieces[i].transform.SetParent(tile.transform);
            blackPieces[i].transform.position = new Vector2(tile.transform.position.x, tile.transform.position.y);
            i++;
        }

        foreach (GameObject tile in tiles)
        {
            tile.GetComponent<Button>().interactable = true;
            tilesDict[tile.name] = new Dictionary<string, GameObject>();
            tilesDict[tile.name]["Tile"] = tile;

            tilesDict[tile.name]["Piece"] = null;

            if (tile.transform.childCount > 0)
            {
                foreach (Transform child in tile.transform)
                {
                    if (child.childCount > 2)
                    {
                        if (child.GetChild(0).name == "Piece")
                        {
                            tilesDict[tile.name]["Piece"] = child.gameObject;
                            break;
                        }
                        else if (child == tile.transform.GetChild(tile.transform.childCount - 1))
                        {
                            tilesDict[tile.name]["Piece"] = null;
                        }

                    }
                    else if (child == tile.transform.GetChild(tile.transform.childCount - 1))
                    {
                        tilesDict[tile.name]["Piece"] = null;
                    }
                }
            }
        }
        print(tilesDict.Count);

        foreach (GameObject item in whitePieces)
        {
            List<GameObject> possMoves = new List<GameObject>();
            BoardDict["White"].Add(item, new Dictionary<string, string>());
            BoardDict["White"][item]["Type"] = item.transform.GetChild(2).GetChild(0).name;
            BoardDict["White"][item]["Tile"] = item.transform.parent.name;



            possMoves = moveFunctions[item.transform.GetChild(2).GetChild(0).name]("White", item.transform.parent.gameObject, false, true);

            possMoves.RemoveAll(x => x == null);

            foreach (GameObject move in possMoves)
            {
                if (!possibleMoveDict["White"].ContainsKey(move.name))
                {
                    possibleMoveDict["White"][move.name] = new Dictionary<GameObject, List<GameObject>>();
                }
                possibleMoveDict["White"][move.name][item] = possMoves;
            }

        }

        foreach (GameObject item in blackPieces)
        {
            List<GameObject> possMoves = new List<GameObject>();
            BoardDict["Black"].Add(item, new Dictionary<string, string>());
            BoardDict["Black"][item]["Type"] = item.transform.GetChild(2).GetChild(0).name;
            BoardDict["Black"][item]["Tile"] = item.transform.parent.name;


            possMoves = moveFunctions[item.transform.GetChild(2).GetChild(0).name]("Black", item.transform.parent.gameObject, false, true);

            possMoves.RemoveAll(x => x == null);

            foreach (GameObject move in possMoves)
            {
                if (!possibleMoveDict["Black"].ContainsKey(move.name))
                {
                    possibleMoveDict["Black"][move.name] = new Dictionary<GameObject, List<GameObject>>();
                }
                possibleMoveDict["Black"][move.name][item] = possMoves;
            }
        }



        dotsOnBoard = false;
    }
    
    public void setPieces()
    {
        if (EventSystem.current.currentSelectedGameObject == blackButton)
        {
            int i = 0;
            foreach (GameObject tile in startTiles)
            {
                blackPieces[i].transform.SetParent(tile.transform);
                blackPieces[i].transform.position = new Vector2(tile.transform.position.x, tile.transform.position.y);

                BoardDict["Black"][blackPieces[i]]["Tile"] = blackPieces[i].transform.parent.name;
                i++;
            }
            i = 0;
            foreach (GameObject tile in oppStartTiles)
            {
                whitePieces[i].transform.SetParent(tile.transform);
                whitePieces[i].transform.position = new Vector2(tile.transform.position.x, tile.transform.position.y);

                BoardDict["White"][whitePieces[i]]["Tile"] = whitePieces[i].transform.parent.name;
                i++;
            }
        }
        else
        {
            int i = 0;
            foreach (GameObject tile in startTiles)
            {
                whitePieces[i].transform.SetParent(tile.transform);
                whitePieces[i].transform.position = new Vector2(tile.transform.position.x, tile.transform.position.y);

                BoardDict["White"][whitePieces[i]]["Tile"] = whitePieces[i].transform.parent.name;
                i++;
            }
            i = 0;
            foreach (GameObject tile in oppStartTiles)
            {
                blackPieces[i].transform.SetParent(tile.transform);
                blackPieces[i].transform.position = new Vector2(tile.transform.position.x, tile.transform.position.y);

                BoardDict["Black"][blackPieces[i]]["Tile"] = blackPieces[i].transform.parent.name;
                i++;
            }
        }


    }

    private void CreateDot(Transform parent)
    {
        GameObject allowedMove = Instantiate(allowedMoveDot, parent);

        allowedMove.transform.localPosition = new Vector3(0, 0, -10000);
        allowedMove.transform.localScale = new Vector2(localTileSize / 2, localTileSize / 2);

        allowedMove.GetComponent<SpriteRenderer>().enabled = true;
    }

    GameObject CheckTile(Dictionary<string, Dictionary<string, GameObject>> tilesDict, string testTileName, string color, bool ignore)
    {
        blocked = false;
        if (tilesDict.ContainsKey(testTileName))
        {
            GameObject newTile = tilesDict[testTileName]["Tile"];
            if (ignore)
            {
                return newTile;
            }

            if (tilesDict[testTileName]["Piece"] != null)
            {
                if (tilesDict[testTileName]["Piece"].transform.GetChild(1).name == color)
                {
                    blocked = true;
                    return null;
                }
                else
                {
                    dotsOnBoard = true;
                    return newTile;
                }
            }
            else
            {
                dotsOnBoard = true;
                return newTile;
            }
        }
        else
        {
            return null;
        }
    }

    // tile == which tile the piece is on when clicked on
    // auto take is only for pawns, only there on others for move function dict
    private List<GameObject> GetPawnMoves(string color, GameObject tile, bool ignore, bool autoTake) // autoTake only for pawns, used to add in the take moves to possMovesDict regardless of if there is a piece to take
    {
        List<bool> canMove = new List<bool>();

        string tileName = tile.name;

        List<GameObject> possibleMoves = new List<GameObject>();


        bool checkTileForMove(string tileName, int tilesAhead)
        {
            char tileNum = tileName[1];
            char newTileNum = 'z';

            if ((color == "White" && whiteStartBot) || (color == "Black" && !whiteStartBot))
            {
                newTileNum = (char)(((int)tileNum) + tilesAhead);
            }
            else
            {
                newTileNum = (char)(((int)tileNum) - tilesAhead);
            }
                
            tileName = tileName.Replace(tileNum, newTileNum);

            foreach (Transform child in board.transform) // child = tile or start pieces container
            {
                if (child.name == tileName && child.childCount > 0)
                {
                    foreach (Transform child2 in child) // piece or text or dot in tile
                    {
                        foreach (Transform child3 in child2) // attributes most likely
                        {
                            if (child3.name == "Piece")
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }

        if ((color == "White" && whiteStartBot) || (color == "Black" && !whiteStartBot))
        {
            canMove.Add(checkTileForMove(tileName, 1));

            if (canMove[0] && (Char.GetNumericValue(tile.name[1]) == 2)) // can move 2 spots cuz its on starting row
            {
                canMove.Add(checkTileForMove(tileName, 2));
            }
            else
            {
                canMove.Add(false);
            }

            if (canMove[0])
            {
                GameObject newTile = tile;
                string newTileName = tileName.Replace(tileName[1], (char)(tileName[1] + 1));
                foreach (GameObject child in tiles)
                {
                    if (child.name == newTileName)
                    {
                        newTile = child;
                    }
                }
                if (newTile == tile)
                {
                    return possibleMoves;
                }


                possibleMoves.Add(newTile);

                bool startPos = false;
                foreach (GameObject startTile in startTiles)
                {
                    if (tile == startTile)
                    {
                        startPos = true;
                    }
                }

                if (startPos && canMove[1])
                {
                    GameObject newTile2 = tile;
                    string newTileName2 = newTileName.Replace(newTileName[1], (char)(newTileName[1] + 1));
                    foreach (GameObject child in tiles)
                    {
                        if (child.name == newTileName2)
                        {
                            newTile2 = child;
                        }
                    }
                    if (newTile == tile)
                    {
                        return possibleMoves;
                    }

                    possibleMoves.Add(newTile2);
                }

                
            }
            // any pieces to be taken ------------------------------------------------------------

            GameObject newTile3 = tile;
            string newTileName3 = tile.name;
            newTileName3 = newTileName3.Replace(newTileName3[0], (char)(newTileName3[0] + 1));
            newTileName3 = newTileName3.Replace(newTileName3[1], (char)(newTileName3[1] + 1));

            // find the tile diagonal to the right
            foreach (GameObject child in tiles)
            {
                if (child.name == newTileName3)
                {
                    newTile3 = child;

                    if (autoTake)
                    {
                        possibleMoves.Add(newTile3);
                    }
                    else
                    {
                        // find the children of said tile
                        foreach (Transform thing in newTile3.transform)
                        {
                            if (thing.childCount > 0)
                            {
                                // find all of the children of the tile that also have children 
                                // pieces have children (the attributes 'Piece' and color)
                                foreach (Transform att in thing)
                                {
                                    // find if there is a piece on the tile
                                    if (att.name == "Piece")
                                    {
                                        Transform oppColor = thing.GetChild(1);

                                        if (oppColor.name == color && ignore)
                                        {
                                            possibleMoves.Add(newTile3);
                                        }

                                        if (oppColor.name != color && oppColor != null)
                                        {
                                            possibleMoves.Add(newTile3);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            GameObject newTile4 = tile;
            string newTileName4 = tile.name;
            newTileName4 = newTileName4.Replace(newTileName4[0], (char)(newTileName4[0] - 1));
            newTileName4 = newTileName4.Replace(newTileName4[1], (char)(newTileName4[1] + 1));

            // find the tile diagonal to the right
            foreach (GameObject child in tiles)
            {
                if (child.name == newTileName4)
                {
                    newTile4 = child;

                    if (autoTake)
                    {
                        possibleMoves.Add(newTile4);
                    }
                    else
                    {
                        // find the children of said tile
                        foreach (Transform thing in newTile4.transform)
                        {
                            if (thing.childCount > 0)
                            {
                                // find all of the children of the tile that also have children 
                                // pieces have children (the attributes 'Piece' and color)
                                foreach (Transform att in thing)
                                {
                                    // find if there is a piece on the tile
                                    if (att.name == "Piece")
                                    {
                                        Transform oppColor2 = thing.GetChild(1);

                                        if (oppColor2.name == color && ignore)
                                        {
                                            possibleMoves.Add(newTile4);
                                        }

                                        if (oppColor2.name != color && oppColor2 != null)
                                        {
                                            possibleMoves.Add(newTile4);
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (!ignore) // maybe?--------------------------------------------------------------------
            {
                dotsOnBoard = true;
            }
        }
        else if ((color == "Black" && whiteStartBot) || (color == "White" && !whiteStartBot))
        {
            canMove.Add(checkTileForMove(tileName, 1));

            if (canMove[0] && (Char.GetNumericValue(tile.name[1]) == 7)) // can move 2 spots cuz its on starting row
            {
                canMove.Add(checkTileForMove(tileName, 2));
            }
            else
            {
                canMove.Add(false);
            }

            if (canMove[0]) {
                GameObject newTile = tile;
                string newTileName = tileName.Replace(tileName[1], (char)(tileName[1] - 1));
                foreach (GameObject child in tiles)
                {
                    if (child.name == newTileName)
                    {
                        newTile = child;
                    }
                }
                if (newTile == tile)
                {
                    return possibleMoves;
                }

                possibleMoves.Add(newTile);

                bool oppStartPos = false;
                foreach (GameObject startTile in oppStartTiles)
                {
                    if (tile == startTile)
                    {
                        oppStartPos = true;
                    }
                }

                if (oppStartPos && canMove[1])
                {
                    GameObject newTile2 = tile;
                    string newTileName2 = newTileName.Replace(newTileName[1], (char)(newTileName[1] - 1));
                    foreach (GameObject child in tiles)
                    {
                        if (child.name == newTileName2)
                        {
                            newTile2 = child;
                        }
                    }
                    if (newTile == tile)
                    {
                        return possibleMoves;
                    }

                    possibleMoves.Add(newTile2);
                }

                
            }
            // any pieces to be taken

            GameObject newTile3 = tile;
            string newTileName3 = tile.name;
            newTileName3 = newTileName3.Replace(newTileName3[0], (char)(newTileName3[0] + 1));
            newTileName3 = newTileName3.Replace(newTileName3[1], (char)(newTileName3[1] - 1));
            // find the tile diagonal to the right
            foreach (GameObject child in tiles)
            {
                if (child.name == newTileName3)
                {
                    newTile3 = child;

                    if (autoTake)
                    {
                        possibleMoves.Add(newTile3);
                    }
                    else
                    {
                        // find the children of said tile
                        foreach (Transform thing in newTile3.transform)
                        {
                            if (thing.childCount > 0)
                            {
                                // find all of the children of the tile that also have children 
                                // pieces have children (the attributes 'Piece' and color)
                                foreach (Transform att in thing)
                                {
                                    // find if there is a piece on the tile
                                    if (att.name == "Piece")
                                    {
                                        Transform oppColor = thing.GetChild(1);

                                        if (oppColor.name == color && ignore)
                                        {
                                            possibleMoves.Add(newTile3);
                                        }

                                        if (oppColor.name != color && oppColor != null)
                                        {
                                            possibleMoves.Add(newTile3);
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
            }

            GameObject newTile4 = tile;
            string newTileName4 = tile.name;
            newTileName4 = newTileName4.Replace(newTileName4[0], (char)(newTileName4[0] - 1));
            newTileName4 = newTileName4.Replace(newTileName4[1], (char)(newTileName4[1] - 1));

            // find the tile diagonal to the right
            foreach (GameObject child in tiles)
            {
                if (child.name == newTileName4)
                {
                    newTile4 = child;

                    if (autoTake)
                    {
                        possibleMoves.Add(newTile4);
                    }
                    else
                    {
                        // find the children of said tile
                        foreach (Transform thing in newTile4.transform)
                        {
                            if (thing.childCount > 0)
                            {
                                // find all of the children of the tile that also have children 
                                // pieces have children (the attributes 'Piece' and color)
                                foreach (Transform att in thing)
                                {
                                    // find if there is a piece on the tile
                                    if (att.name == "Piece")
                                    {
                                        Transform oppColor2 = thing.GetChild(1);

                                        if (oppColor2.name == color && ignore)
                                        {
                                            possibleMoves.Add(newTile4);
                                        }

                                        if (oppColor2.name != color && oppColor2 != null)
                                        {
                                            possibleMoves.Add(newTile4);
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (!ignore)
            {
                dotsOnBoard = true;
            }
        }
        return possibleMoves;
    }

    private List<GameObject> GetRookMoves(string color, GameObject tile, bool ignore, bool autoTake)
    {
        char tileChar = tile.name[0];
        char tileNum = tile.name[1];
        char newTileChar = tileChar;
        char newTileNum = tileNum;
        string newTileName = tile.name;
        int ignoredTiles = 0;

        bool dirComparison = false; // used for the while statement so that I can just use a for loop to repeat 4 times
        bool laneBlocked = false;

        List<GameObject> possibleMoves = new List<GameObject>();

        for (int i = 0; i < 4; i++)
        {
            newTileChar = tileChar;
            newTileNum = tileNum;
            newTileName = tile.name;
            dirComparison = false;
            ignoredTiles = 0;

            laneBlocked = false;

            if (i == 0)
            {
                dirComparison = (int)(char.GetNumericValue(newTileNum)) < 8 && !laneBlocked;
            }
            else if (i == 1)
            {
                dirComparison = (int)(char.GetNumericValue(newTileNum)) > 1 && !laneBlocked;
            }
            else if (i == 2)
            {
                dirComparison = newTileChar >= 'A' && !laneBlocked;
            }
            else
            {
                dirComparison = newTileChar <= 'H' && !laneBlocked;
            }

            while (dirComparison)
            {
                if (i == 0)
                {
                    newTileNum++;
                    dirComparison = (int)(char.GetNumericValue(newTileNum)) < 8 && !laneBlocked;
                }
                else if (i == 1)
                {
                    newTileNum--;
                    dirComparison = (int)(char.GetNumericValue(newTileNum)) > 1 && !laneBlocked;
                }
                else if (i == 2)
                {
                    newTileChar++;
                    dirComparison = newTileChar >= 'A' && !laneBlocked;
                }
                else
                {
                    newTileChar--;
                    dirComparison = newTileChar <= 'H' && !laneBlocked;
                }

                newTileName = newTileChar.ToString() + newTileNum.ToString();
                try
                {
                    if (tilesDict.ContainsKey(newTileName))
                    {
                        GameObject tileObj = tilesDict[newTileName]["Tile"];

                        if (tilesDict[newTileName]["Piece"] != null)
                        {
                            if (tilesDict[newTileName]["Piece"].transform.GetChild(1).name == color)
                            {
                                if (ignore && ignoredTiles == 0)
                                {
                                    ignoredTiles += 1;
                                    possibleMoves.Add(tileObj);
                                    dotsOnBoard = true;
                                }
                                laneBlocked = true;
                            }
                            else
                            {
                                possibleMoves.Add(tileObj);
                                dotsOnBoard = true;
                                laneBlocked = true;
                            }
                        }
                        if (laneBlocked)
                        {
                            break;
                        }
                        else
                        {
                            possibleMoves.Add(tileObj);
                            dotsOnBoard = true;
                        }
                        dirComparison = !laneBlocked && (newTileChar >= 'A' && newTileChar <= 'H') && (int)(char.GetNumericValue(newTileNum)) >= 1 && (int)(char.GetNumericValue(newTileNum)) <= 8;
                    }
                }
                catch
                {
                    print(9292929929292);
                }

            }
        }
        return possibleMoves;
    }

    private List<GameObject> GetBishopMoves(string color, GameObject tile, bool ignore, bool autoTake)
    {
        char tileChar = tile.name[0];
        char tileNum = tile.name[1];
        char newTileChar = tileChar;
        char newTileNum = tileNum;
        string newTileName = tile.name;
        int ignoredTiles = 0;

        bool canMove = false; // used for the while statement so that I can just use a for loop to repeat 4 times
        bool laneBlocked = false;

        List<GameObject> possibleMoves = new List<GameObject>();

        for (int i = 0; i < 4; i++)
        {
            newTileChar = tileChar;
            newTileNum = tileNum;
            newTileName = tile.name;
            canMove = false;
            ignoredTiles = 0;

            laneBlocked = false;

            if (i == 0)
            {
                canMove = (int)(char.GetNumericValue(newTileNum)) < 8 && !laneBlocked && newTileChar <= 'H';
            }
            else if (i == 1)
            {
                canMove = (int)(char.GetNumericValue(newTileNum)) > 1 && !laneBlocked && newTileChar >= 'A';
            }
            else if (i == 2)
            {
                canMove = (int)(char.GetNumericValue(newTileNum)) < 8 && !laneBlocked && newTileChar >= 'A';
            }
            else
            {
                canMove = (int)(char.GetNumericValue(newTileNum)) > 1 && !laneBlocked && newTileChar <= 'H';
            }

            while (canMove)
            {
                if (i == 0)
                {
                    newTileNum++;
                    newTileChar--;
                    canMove = (int)(char.GetNumericValue(newTileNum)) < 8 && !laneBlocked && newTileChar <= 'H';
                }
                else if (i == 1)
                {
                    newTileNum--;
                    newTileChar++;
                    canMove = (int)(char.GetNumericValue(newTileNum)) > 1 && !laneBlocked && newTileChar >= 'A';
                }
                else if (i == 2)
                {
                    newTileNum++;
                    newTileChar++;
                    canMove = (int)(char.GetNumericValue(newTileNum)) < 8 && !laneBlocked && newTileChar >= 'A';
                }
                else
                {
                    newTileNum--;
                    newTileChar--;
                    canMove = (int)(char.GetNumericValue(newTileNum)) > 1 && !laneBlocked && newTileChar <= 'H';
                }

                newTileName = newTileChar.ToString() + newTileNum.ToString();

                try
                {
                    if (tilesDict.ContainsKey(newTileName))
                    {
                        GameObject tileObj = tilesDict[newTileName]["Tile"];

                        if (tilesDict[newTileName]["Piece"] != null)
                        {
                            if (tilesDict[newTileName]["Piece"].transform.GetChild(1).name == color)
                            {
                                if (ignore && ignoredTiles == 0)
                                {
                                    ignoredTiles += 1;
                                    possibleMoves.Add(tileObj);
                                    dotsOnBoard = true;
                                }
                                laneBlocked = true;
                            }
                            else
                            {
                                possibleMoves.Add(tileObj);
                                dotsOnBoard = true;
                                laneBlocked = true;
                            }
                        }
                        if (laneBlocked)
                        {
                            break;
                        }
                        else
                        {
                            possibleMoves.Add(tileObj);
                            dotsOnBoard = true;
                        }
                        canMove = !laneBlocked && (newTileChar >= 'A' && newTileChar <= 'H') && (int)(char.GetNumericValue(newTileNum)) >= 1 && (int)(char.GetNumericValue(newTileNum)) <= 8;
                    }
                }
                catch
                {
                    print(000000000000000000000000000000000000000);
                }

            }
        }
        return possibleMoves;
    }

    private List<GameObject> GetKnightMoves(string color, GameObject tile, bool ignore, bool autoTake)
    {
        char tileChar = tile.name[0];
        char tileNum = tile.name[1];
        char newTileChar = tileChar;
        char newTileNum = tileNum;
        string newTileName = tile.name;

        List<GameObject> possibleMoves = new List<GameObject>();

        

        if ((int)(char.GetNumericValue(tileNum)) < 8)
        {
            if (tileChar < 'H')
            {
                if ((int)(char.GetNumericValue(tileNum)) < 7)
                {
                    newTileNum++;
                    newTileNum++;
                    newTileChar++;

                    newTileName = newTileChar.ToString() + newTileNum.ToString();
                    possibleMoves.Add(CheckTile(tilesDict, newTileName, color, ignore));
                }
                if (tileChar < 'G')
                {
                    newTileNum = tileNum;
                    newTileChar = tileChar;

                    newTileNum++;
                    newTileChar++;
                    newTileChar++;

                    newTileName = newTileChar.ToString() + newTileNum.ToString();
                    possibleMoves.Add(CheckTile(tilesDict, newTileName, color, ignore));
                }
            }
            if (tileChar > 'A')
            {
                if ((int)(char.GetNumericValue(tileNum)) < 8)
                {
                    newTileNum = tileNum;
                    newTileChar = tileChar;

                    newTileNum++;
                    newTileNum++;
                    newTileChar--;

                    newTileName = newTileChar.ToString() + newTileNum.ToString();
                    possibleMoves.Add(CheckTile(tilesDict, newTileName, color, ignore));
                }
                if (tileChar > 'B')
                {
                    newTileNum = tileNum;
                    newTileChar = tileChar;

                    newTileNum++;
                    newTileChar--;
                    newTileChar--;

                    newTileName = newTileChar.ToString() + newTileNum.ToString();
                    possibleMoves.Add(CheckTile(tilesDict, newTileName, color, ignore));
                }
            }
        }
        if ((int)(char.GetNumericValue(tileNum)) > 1)
        {
            if (tileChar < 'H')
            {
                if ((int)(char.GetNumericValue(tileNum)) > 2)
                {
                    newTileNum = tileNum;
                    newTileChar = tileChar;

                    newTileNum--;
                    newTileNum--;
                    newTileChar++;

                    newTileName = newTileChar.ToString() + newTileNum.ToString();
                    possibleMoves.Add(CheckTile(tilesDict, newTileName, color, ignore));
                }
                if (tileChar < 'G')
                {
                    newTileNum = tileNum;
                    newTileChar = tileChar;

                    newTileNum--;
                    newTileChar++;
                    newTileChar++;

                    newTileName = newTileChar.ToString() + newTileNum.ToString();
                    possibleMoves.Add(CheckTile(tilesDict, newTileName, color, ignore));
                }
            }
            if (tileChar > 'A')
            {
                if ((int)(char.GetNumericValue(tileNum)) > 2)
                {
                    newTileNum = tileNum;
                    newTileChar = tileChar;

                    newTileNum--;
                    newTileNum--;
                    newTileChar--;

                    newTileName = newTileChar.ToString() + newTileNum.ToString();
                    possibleMoves.Add(CheckTile(tilesDict, newTileName, color, ignore));
                }

            }
            if (tileChar > 'B')
            {
                newTileNum = tileNum;
                newTileChar = tileChar;

                newTileNum--;
                newTileChar--;
                newTileChar--;

                newTileName = newTileChar.ToString() + newTileNum.ToString();
                possibleMoves.Add(CheckTile(tilesDict, newTileName, color, ignore));
            }


        }
        blocked = false;
        return possibleMoves;
    }

    private List<GameObject> GetKingMoves(string color, GameObject tile, bool ignore, bool autoTake)
    {
        char tileChar = tile.name[0];
        char tileNum = tile.name[1];
        char newTileChar = tileChar;
        char newTileNum = tileNum;
        string newTileName = tile.name;

        List<GameObject> possibleMoves = new List<GameObject>();

        newTileNum--;
        newTileChar--;

        for (int i = 0; i < 8; i++)
        {
            if (i == 3)
            {
                newTileNum--;
                newTileNum--;
                newTileNum--;
                newTileChar++;
                newTileChar++;
            }
            else if (i == 6)
            {
                newTileNum--;
                newTileChar--;
            }
            else if (i == 7)
            {
                newTileNum--;
                newTileNum--;
                newTileNum--;
            }

            newTileName = newTileChar.ToString() + newTileNum.ToString();

            possibleMoves.Add(CheckTile(tilesDict, newTileName, color, ignore));
            newTileNum++;
        }

        newTileNum = tileNum;
        newTileChar = tileChar;
        newTileName = tile.name;
        GameObject newTile = null;

        void resetValues()
        {
            newTileNum = tileNum;
            newTileChar = tileChar;
            newTileName = tile.name;
            newTile = null;
        }

        void checkForCastle(bool add)
        {
            if (add)
            {
                newTileChar++;
            }
            else
            {
                newTileChar--;
            }

            newTileName = newTileChar.ToString() + newTileNum.ToString();
            newTile = CheckTile(tilesDict, newTileName, color, false);
            if (newTile != null && newTile.name == newTileName)
            {
                if (add)
                {
                    newTileChar++;
                }
                else
                {
                    newTileChar--;
                }
                newTileName = newTileChar.ToString() + newTileNum.ToString();
                newTile = CheckTile(tilesDict, newTileName, color, false);

                if (newTile != null && newTile.name == newTileName)
                {
                    possibleMoves.Add(newTile);
                    castle = true;
                }
            }
            resetValues();
        }

        foreach (Transform child in tile.transform)
        {
            if (child.childCount > 2)
            {
                try
                {
                    if (child.GetChild(2).GetChild(0).name == "King")
                    {
                        if (!hasMovedDict[child.GetChild(1).name + child.GetChild(2).GetChild(0).name])
                        {
                            if (color == "White" && !whiteInCheck)
                            {
                                if (!hasMovedDict["WhiteRook1"])
                                {
                                    checkForCastle(false);

                                }
                                if (!hasMovedDict["WhiteRook2"])
                                {
                                    checkForCastle(true);

                                }
                            }
                            else if (color == "Black" && !blackInCheck)
                            {
                                if (!hasMovedDict["BlackRook1"])
                                {
                                    checkForCastle(false);
                                }
                                if (!hasMovedDict["BlackRook2"])
                                {
                                    checkForCastle(true);

                                }
                            }
                        }
                    }
                } catch (KeyNotFoundException e)
                {
                    print(child.name);
                    print(e);
                }

            }
        }
        return possibleMoves;
    }

    private List<GameObject> GetQueenMoves(string color, GameObject tile, bool ignore, bool autoTake)
    {
        List<GameObject> possibleMoves = new List<GameObject>();
        possibleMoves.AddRange(GetRookMoves(color, tile, ignore, false));
        possibleMoves.AddRange(GetBishopMoves(color, tile, ignore, false)); 

        return possibleMoves;
    }

    private List<GameObject> GetMoves(string color, GameObject tile, GameObject playerPiece)
    {
        // get piece
        // get moves
        // move to all possible moves
        // check all possible opponent moves to see if player's king is on one of them during the step above
        // remove every move that causes check
        // return new possible move list

        List<GameObject> possibleMoves = new List<GameObject>();
        List<GameObject> newPossMoves = new List<GameObject>();


        List<GameObject> possibleOppMoves = new List<GameObject>();
        List<GameObject> affectedOppPieces = new List<GameObject>();

        Transform origParent = playerPiece.transform.parent;

        bool invalid = false;
        string oppColor = "";

        if (color == "White")
        {
            oppColor = "Black";
        }
        else if (color == "Black")
        {
            oppColor = "White";
        }

        try
        {
            possibleMoves = moveFunctions[BoardDict[color][playerPiece]["Type"]](color, tile, false, false);
        }
        catch (KeyNotFoundException e)
        {
            print(BoardDict[color][playerPiece]["Type"]);
            print(e);
        }
        possibleMoves.RemoveAll(x => x == null);

        
        List<GameObject> possProtMoves = new List<GameObject>();
        possProtMoves = moveFunctions["Queen"](color, tile, true, false);
        possProtMoves.RemoveAll(x => x == null);
        bool protecting = false;
        bool take = false;

        foreach (GameObject move in possProtMoves)
        {
            if (playerPiece.transform.GetChild(2).GetChild(0).name == "King")
            {
                protecting = true;
                break;
            }

            if (tilesDict[move.name]["Piece"] != null)
            {
                if (tilesDict[move.name]["Piece"].transform.GetChild(1).name == color && tilesDict[move.name]["Piece"].transform.GetChild(2).GetChild(0).name == "King")
                {

                    if (possibleMoveDict[oppColor].ContainsKey(tile.name))
                    {
                        print('c');
                        protecting = true;
                    }
                }
            }
            
        }

        foreach (GameObject possMove in possibleMoves)
        {
            take = false;
            if ((whiteInCheck && color == "White") || (blackInCheck && color == "Black"))
            {
                string kingInCheck = "";
                GameObject king = null;
                if (whiteInCheck)
                {
                    kingInCheck = "White";
                    king = whitePieces[4];
                }
                else if (blackInCheck)
                {
                    kingInCheck = "Black";
                    king = blackPieces[4];
                }
                else
                {
                    print("Unexpected error 1305ish");
                }

                playerPiece.transform.SetParent(possMove.transform);
                BoardDict[color][playerPiece]["Tile"] = possMove.name;

                GameObject prevPieceOnTile = null;
                prevPieceOnTile = tilesDict[possMove.name]["Piece"];


                tilesDict[possMove.name]["Piece"] = playerPiece;
                tilesDict[tile.name]["Piece"] = null;

                //print("NewCheckVals");
                //print(tilesDict[possMove.name]["Piece"].name);
                //print(BoardDict[color][playerPiece]["Tile"]);

                if (possibleMoveDict[oppColor].ContainsKey(BoardDict[kingInCheck][king]["Tile"]))
                {
                    //print(1);
                    foreach (GameObject oppPiece in possibleMoveDict[oppColor][BoardDict[kingInCheck][king]["Tile"]].Keys.Distinct())
                    {
                        if (BoardDict[oppColor].ContainsKey(oppPiece) && oppPiece.transform.parent == possMove.transform)
                        {
                            take = true;
                        }

                        if (BoardDict[oppColor].ContainsKey(oppPiece))
                        {
                            //print(2);
                            possibleOppMoves.AddRange(moveFunctions[BoardDict[oppColor][oppPiece]["Type"]](oppColor, oppPiece.transform.parent.gameObject, true, true));
                        }
                    }
                }

                foreach (GameObject oppMove in possibleOppMoves)
                {

                    if (tilesDict[oppMove.name]["Piece"] != null)
                    {
                        //print(3);
                        if (tilesDict[oppMove.name]["Piece"].transform.GetChild(1).name == color && tilesDict[oppMove.name]["Piece"].transform.GetChild(2).GetChild(0).name == "King")
                        {
                            //print(4);
                            if (take)
                            {
                                if (!possibleOppMoves.Contains(possMove))
                                {
                                    //print(5);
                                    invalid = false;
                                    break;
                                }
                            }

                            invalid = true;
                            break;
                        }
                    }
                }

                possibleOppMoves.Clear();

                possibleOppMoves.RemoveAll(x => x == null);






                BoardDict[color][playerPiece]["Tile"] = tile.name;
                tilesDict[tile.name]["Piece"] = playerPiece;
                tilesDict[possMove.name]["Piece"] = prevPieceOnTile;

                if (!invalid)
                {
                    newPossMoves.Add(possMove);
                }
                else
                {
                    invalid = false;
                }
            }
            else if (protecting)
            {

                playerPiece.transform.SetParent(possMove.transform);
                BoardDict[color][playerPiece]["Tile"] = possMove.name;

                GameObject prevPieceOnTile = null;
                prevPieceOnTile = tilesDict[possMove.name]["Piece"];


                tilesDict[possMove.name]["Piece"] = playerPiece;
                tilesDict[tile.name]["Piece"] = null;

               

                //print("NewVals");
                //print(tilesDict[possMove.name]["Piece"].name);
                //print(BoardDict[color][playerPiece]["Tile"]);
                //print(playerPiece.transform.parent.name);

                string newName = tile.name;

                if (playerPiece.transform.GetChild(2).GetChild(0).name == "King")
                {
                    newName = possMove.name;
                }


                if (possibleMoveDict[oppColor].ContainsKey(newName))
                {
                    foreach (GameObject oppPiece in possibleMoveDict[oppColor][newName].Keys.Distinct())
                    {
                        if (BoardDict[oppColor].ContainsKey(oppPiece) && oppPiece.transform.parent == possMove.transform)
                        {
                            take = true;
                        }
                        //print(tilesDict["F7"]["Piece"].name);

                        //print(oppPiece.name);
                        //print(BoardDict[oppColor].ContainsKey(oppPiece));
                        if (BoardDict[oppColor].ContainsKey(oppPiece))
                        {
                            possibleOppMoves.AddRange(moveFunctions[BoardDict[oppColor][oppPiece]["Type"]](oppColor, oppPiece.transform.parent.gameObject, true, true));
                        }
                    }



                    possibleOppMoves.RemoveAll(x => x == null);

                }

                foreach (GameObject oppMove in possibleOppMoves)
                {
                    //print(oppMove.name);

                    if (tilesDict[oppMove.name]["Piece"] != null)
                    {
                        //print(1);
                        if (tilesDict[oppMove.name]["Piece"].transform.GetChild(1).name == color && tilesDict[oppMove.name]["Piece"].transform.GetChild(2).GetChild(0).name == "King")
                        {
                            //print(2);
                            if (take)
                            {
                                //print(3);
                                if (!possibleOppMoves.Contains(possMove))
                                {
                                    invalid = false;
                                    break;
                                }
                            }

                            invalid = true;
                            break;
                        }
                    }
                }

                possibleOppMoves.Clear();
                BoardDict[color][playerPiece]["Tile"] = tile.name;
                tilesDict[tile.name]["Piece"] = playerPiece;
                tilesDict[possMove.name]["Piece"] = prevPieceOnTile;

                if (!invalid)
                {
                    newPossMoves.Add(possMove);
                }
                else
                {
                    invalid = false;
                }
            }
            else if ((!whiteInCheck && color == "White") || (!blackInCheck && color == "Black"))
            {
                //print("aaaaa");
                newPossMoves.Add(possMove);
            }
        }
        playerPiece.transform.SetParent(origParent);


        newPossMoves.RemoveAll(x => x == null);
        if (newPossMoves.Count > 0)
        {
            dotsOnBoard = true;
        }
        else
        {
            dotsOnBoard = false;
        }

        return newPossMoves;
    }

    void win(string color, bool winCon)
    {
        print("won");
        string oppColor = "";

        if (color == "White")
        {
            oppColor = "Black";
        }
        else
        {
            oppColor = "White";
        }

        foreach (GameObject tile in tiles)
        {
            tile.GetComponent<Button>().interactable = false;
        }

        if (winCon)
        {
            winScreen.transform.GetChild(0).GetChild(0).GetComponent<TextMeshPro>().text = "Checkmate";
            winScreen.transform.GetChild(0).GetChild(1).GetComponent<TextMeshPro>().text = oppColor + " Wins!";
        }
        else
        {
            winScreen.transform.GetChild(0).GetChild(0).GetComponent<TextMeshPro>().text = "Stalemate";
            winScreen.transform.GetChild(0).GetChild(1).GetComponent<TextMeshPro>().text = "No One Wins!";
        }
        winScreen.SetActive(true);
    }

    public void movePieces()
    {
        GameObject tile = EventSystem.current.currentSelectedGameObject; // tile
        string color = "";

        List<GameObject> moves = new List<GameObject>();

        if (dotsOnBoard)
        {
            GameObject[] placedDots = GameObject.FindGameObjectsWithTag("AllowedMove");
            int i = 0;

            sameLoopBlack = false;
            sameLoopWhite = false;

            foreach (GameObject dot in placedDots)
            {
                i++;
                foreach (Transform child in tile.transform)
                {
                    if (piece != null)
                    {
                        // find if there is a piece of the opposite color on the dot and get rid of it if so
                        if (child.childCount > 2) // piece has mult children
                        {
                            if (child.GetChild(0).name == "Piece")
                            {
                                if (child.GetChild(1).name != piece.transform.GetChild(1).name) // if the color of the piece on the dot is NOT the same as the color of the players piece
                                {
                                    if (tile.transform.Find(dot.name) != null)
                                    {
                                        if (BoardDict.ContainsKey(piece.transform.GetChild(1).name) && BoardDict[child.GetChild(1).name].ContainsKey(child.gameObject))
                                        {
                                            BoardDict[child.GetChild(1).name].Remove(child.gameObject);
                                        }
                                        tilesDict[child.parent.name]["Piece"] = null;
                                        //possibleMoveDict[child.GetChild(1).name][child.parent.name].Remove(child.gameObject);
                                        Destroy(child.gameObject);
                                    }
                                }
                            } 
                        }
                        // move the piece to the dot
                        if (child == dot.transform)
                        {
                            if (piece.transform.parent.name == "D2")
                            {
                                print("D2 - after");
                            }
                            tilesDict[piece.transform.parent.name]["Piece"] = null;

                            piece.transform.SetParent(tile.transform);
                            piece.transform.localPosition = new Vector3(0, 0, -9720);

                            // setting piece to new tile
                            tilesDict[tile.name]["Piece"] = piece;
                            BoardDict[piece.transform.GetChild(1).name][piece]["Tile"] = tile.name;
                            
                            foreach (GameObject key in BoardDict["White"].Keys) {
                                List<GameObject> possibleMoves = moveFunctions[key.transform.GetChild(2).GetChild(0).name]("White", key.transform.parent.gameObject, false, true);
                                possibleMoves.RemoveAll(x => x == null);

                                foreach (GameObject move in possibleMoves)
                                {
                                    if (!possibleMoveDict["White"].ContainsKey(move.name))
                                    {
                                        possibleMoveDict["White"][move.name] = new Dictionary<GameObject, List<GameObject>>();
                                    }
                                    possibleMoveDict["White"][move.name][key] = possibleMoves;
                                }
                            }
                            foreach (GameObject key in BoardDict["Black"].Keys)
                            {
                                List<GameObject> possibleMoves = moveFunctions[key.transform.GetChild(2).GetChild(0).name]("Black", key.transform.parent.gameObject, false, true);
                                possibleMoves.RemoveAll(x => x == null);

                                foreach (GameObject move in possibleMoves)
                                {
                                    if (!possibleMoveDict["Black"].ContainsKey(move.name))
                                    {
                                        possibleMoveDict["Black"][move.name] = new Dictionary<GameObject, List<GameObject>>();
                                    }
                                    possibleMoveDict["Black"][move.name][key] = possibleMoves;
                                }
                            }


                            if (piece.transform.childCount > 3) // rooks have moved
                            {
                                if (hasMovedDict.ContainsKey(piece.transform.GetChild(1).name + piece.transform.GetChild(2).GetChild(0).name + piece.transform.GetChild(3).name))
                                {
                                    hasMovedDict[piece.transform.GetChild(1).name + piece.transform.GetChild(2).GetChild(0).name + piece.transform.GetChild(3).name] = true;
                                }
                            }
                            else // kings have moved
                            {
                                if (hasMovedDict.ContainsKey(piece.transform.GetChild(1).name + piece.transform.GetChild(2).GetChild(0).name))
                                {
                                    hasMovedDict[piece.transform.GetChild(1).name + piece.transform.GetChild(2).GetChild(0).name] = true;
                                }
                            }
                            if (castle) { // moving rooks on castle
                                if (tile.name == "C1")
                                {
                                    if (tilesDict["A1"]["Piece"].transform.GetChild(2).GetChild(0).name == "Rook")
                                    {
                                        tilesDict["A1"]["Piece"].transform.SetParent(tilesDict["D1"]["Tile"].transform);
                                        tilesDict["D1"]["Piece"] = tilesDict["A1"]["Piece"];
                                        tilesDict["A1"]["Piece"] = null;
                                        tilesDict["D1"]["Piece"].transform.localPosition = new Vector3(0, 0, -9720);
                                    }
                                }
                                else if (tile.name == "G1")
                                {
                                    if (tilesDict["H1"]["Piece"].transform.GetChild(2).GetChild(0).name == "Rook")
                                    {
                                        tilesDict["H1"]["Piece"].transform.SetParent(tilesDict["F1"]["Tile"].transform);
                                        tilesDict["F1"]["Piece"] = tilesDict["H1"]["Piece"];
                                        tilesDict["H1"]["Piece"] = null;
                                        tilesDict["F1"]["Piece"].transform.localPosition = new Vector3(0, 0, -9720);
                                    }
                                }
                                else if (tile.name == "C8")
                                {
                                    if (tilesDict["A8"]["Piece"].transform.GetChild(2).GetChild(0).name == "Rook")
                                    {
                                        tilesDict["A8"]["Piece"].transform.SetParent(tilesDict["D8"]["Tile"].transform);
                                        tilesDict["D8"]["Piece"] = tilesDict["A8"]["Piece"];
                                        tilesDict["A8"]["Piece"] = null;
                                        tilesDict["D8"]["Piece"].transform.localPosition = new Vector3(0, 0, -9720);
                                    }
                                }
                                else if (tile.name == "G8")
                                {
                                    if (tilesDict["H8"]["Piece"].transform.GetChild(2).GetChild(0).name == "Rook")
                                    {
                                        tilesDict["H8"]["Piece"].transform.SetParent(tilesDict["F8"]["Tile"].transform);
                                        tilesDict["F8"]["Piece"] = tilesDict["H8"]["Piece"];
                                        tilesDict["H8"]["Piece"] = null;
                                        tilesDict["F8"]["Piece"].transform.localPosition = new Vector3(0, 0, -9720);
                                    }
                                }
                            }

                            if (piece.transform.GetChild(1).name == "White")
                            {
                                turn = "Black";
                            }
                            else if (piece.transform.GetChild(1).name == "Black")
                            {
                                turn = "White";
                            }

                            List<GameObject> possMoves = new List<GameObject>();
                            possMoves.AddRange(moveFunctions[piece.transform.GetChild(2).GetChild(0).name](piece.transform.GetChild(1).name, tile, false, false));
                            possMoves.RemoveAll(x => x == null);

                            /*
                            print(piece.transform.GetChild(2).GetChild(0).name);
                            print(piece.transform.GetChild(0).name);
                            print(tile.name);
                            print(possMoves.Count);
                            */
                            print("same loop: " + sameLoopBlack);
                            foreach (GameObject move in possMoves)
                            {
                                if (move.transform.childCount > 0)
                                {
                                    foreach (Transform item in move.transform)
                                    {
                                        if (item.childCount > 2)
                                        {
                                            if (item.GetChild(0).name == "Piece")
                                            {
                                                if (item.GetChild(2).GetChild(0).name == "King")
                                                {
                                                    if (item.GetChild(1).name == "White" && piece.transform.GetChild(1).name != "White")
                                                    {
                                                        whiteInCheck = true;
                                                        sameLoopWhite = true;
                                                        print(1);
                                                    }
                                                    else if (item.GetChild(1).name == "Black" && piece.transform.GetChild(1).name != "Black")
                                                    {
                                                        blackInCheck = true;
                                                        sameLoopBlack = true;
                                                        print(1);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            print("same loop: " + sameLoopBlack);

                            string calor = "";
                            for (i = 0; i < 2; i++) 
                            {
                                if (i == 0)
                                {
                                    calor = "White";
                                }
                                else
                                {
                                    calor = "Black";
                                }

                                moves.Clear();
                                foreach (GameObject key in BoardDict[calor].Keys)
                                {
                                    if (key != null)
                                    {
                                        moves.AddRange(GetMoves(calor, key.transform.parent.gameObject, key));
                                        moves.RemoveAll(x => x == null);
                                    }
                                }

                                // if no moves and in check, opp wins
                                if (moves.Count == 0)
                                {
                                    if (whiteInCheck || blackInCheck)
                                    {
                                        win(turn, true);
                                        break;
                                    }
                                    else
                                    {
                                        win(turn, false);
                                        break;
                                    }
                                    
                                }
                            }
                            dotsOnBoard = false;

                            print(blackInCheck);
                            piece = null;
                            print("same loop: " + sameLoopBlack);

                            if (whiteInCheck && !sameLoopWhite)
                            {
                                whiteInCheck = false;
                                print(2);
                            }
                            else if (blackInCheck && !sameLoopBlack)
                            {
                                blackInCheck = false;
                                print(4);
                            }

                                
                            sameLoopWhite = false;
                            sameLoopBlack = false;
                            dotsOnBoard = false;

                            print(blackInCheck);
                            }
                    }
                    
                }
                if (dot.transform.parent != allowedMoveDot.transform.parent) // if not original dot that im duplicating
                {
                    Destroy(dot);
                }

                if (i == (placedDots.Count()))
                {


                    sameLoopWhite = false;
                    sameLoopBlack = false;
                    dotsOnBoard = false;
                    print(3);
                    return;
                }
            }

        }

        castle = false;

        foreach (Transform child in tile.transform) // get children of tile (piece,text, etc.)
        {
            if (child.childCount > 2) // checks for children so no error. if it has at least 2, its prolly a piece but double checking below
            {
                if (child.GetChild(0).name == "Piece")
                {
                    piece = child.gameObject;
                    color = child.GetChild(1).name;
                }
            }
        }


        if (color != turn)
        {
            piece = null;
            return;
        }


        moves.Clear();
        if (piece != null)
        {
            moves = GetMoves(color, tile, piece);

            moves.RemoveAll(x => x == null);
        }
       

        

        foreach (GameObject move in moves)
        {
            // if win disable all tiles w/ function
            if (move != null)
            {
                CreateDot(move.transform);
            }

            
        }

        

        
    }



    public void ChangeColor()
    {
        GameObject selectedObj = EventSystem.current.currentSelectedGameObject;
        Color yellow = new Color(255,255,0);
        Color green = new Color(111, 140, 93);
        Color white = new Color(255,255,255);


        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i].GetComponent<Image>().color == yellow )
            {
                if ((i % 2) == 0)
                {
                    tiles[i].GetComponent<Image>().color = white;
                }
                else
                {
                    tiles[i].GetComponent<Image>().color = green;
                }
            }
        }
        EventSystem.current.currentSelectedGameObject.GetComponent<Image>().color = yellow;
    }


}


// TO DO

/* 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 *  
 */