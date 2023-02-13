using System.Collections.Generic;
using UnityEngine;

public class BspGenerator
{
    public BspGenerator(BSP_Asset bsp)
    {
        this.bsp = bsp;
        SubDungeon rootSubDungeon = new SubDungeon(new Rect(0, 0, bsp.gridW, bsp.gridH));
        CreateBSP(rootSubDungeon);
        rootSubDungeon.CreateRoom();

        grid = new char[bsp.gridW, bsp.gridH];
        for (int i = 0; i < bsp.gridW; i++) for (int j = 0; j < bsp.gridH; j++) grid[i, j] = bsp.wall;
        DrawRooms(rootSubDungeon);
        Drawaisle(rootSubDungeon);
    }
    private BSP_Asset bsp;
    public char[,] grid { private set; get; }
    public class SubDungeon
    {
        public SubDungeon left, right;
        public Rect rect;
        public Rect room = new Rect(-1, -1, 0, 0); // i.e null
        public List<Rect> aisle = new List<Rect>();

        public SubDungeon(Rect mrect)
        {
            rect = mrect;
        }

        public bool IAmLeaf()
        {
            return left == null && right == null;
        }

        public bool Split(int minRoomSize, int maxRoomSize)
        {
            if (!IAmLeaf()) return false;

            // choose a vertical or horizontal split depending on the proportions
            // i.e. if too wide split vertically, or too long horizontally, 
            // or if nearly square choose vertical or horizontal at random
            bool splitH;
            if (rect.width / rect.height >= 1.25) splitH = false;

            else if (rect.height / rect.width >= 1.25) splitH = true;

            else splitH = Random.Range(0.0f, 1.0f) > 0.5;

            if (Mathf.Min(rect.height, rect.width) / 2 < minRoomSize) return false;
            if (splitH)
            {
                // split so that the resulting sub-dungeons widths are not too small
                // (since we are splitting horizontally) 
                int split = Random.Range(minRoomSize, (int)(rect.width - minRoomSize));

                left = new SubDungeon(new Rect(rect.x, rect.y, rect.width, split));
                right = new SubDungeon(
                    new Rect(rect.x, rect.y + split, rect.width, rect.height - split));
            }
            else
            {
                int split = Random.Range(minRoomSize, (int)(rect.height - minRoomSize));

                left = new SubDungeon(new Rect(rect.x, rect.y, split, rect.height));
                right = new SubDungeon(
                    new Rect(rect.x + split, rect.y, rect.width - split, rect.height));
            }
            return true;
        }

        public void CreateRoom()
        {
            if (left != null)
            {
                left.CreateRoom();
            }
            if (right != null)
            {
                right.CreateRoom();
            }
            if (left != null && right != null)
            {
                CreateCorridorBetween(left, right);
            }
            if (IAmLeaf())
            {
                int roomWidth = (int)Random.Range(rect.width / 2, rect.width - 2);
                int roomHeight = (int)Random.Range(rect.height / 2, rect.height - 2);
                int roomX = (int)Random.Range(1, rect.width - roomWidth - 1);
                int roomY = (int)Random.Range(1, rect.height - roomHeight - 1);

                // room position will be absolute in the board, not relative to the sub-dungeon
                room = new Rect(rect.x + roomX, rect.y + roomY, roomWidth, roomHeight);
            }
        }
        public void CreateCorridorBetween(SubDungeon left, SubDungeon right)
        {
            Rect lroom = left.GetRoom();
            Rect rroom = right.GetRoom();

            // attach the corridor to a random point in each room
            Vector2 lpoint = new Vector2((int)Random.Range(lroom.x + 1, lroom.xMax - 1), (int)Random.Range(lroom.y + 1, lroom.yMax - 1));
            Vector2 rpoint = new Vector2((int)Random.Range(rroom.x + 1, rroom.xMax - 1), (int)Random.Range(rroom.y + 1, rroom.yMax - 1));

            // always be sure that left point is on the left to simplify the code
            if (lpoint.x > rpoint.x)
            {
                Vector2 temp = lpoint;
                lpoint = rpoint;
                rpoint = temp;
            }

            int w = (int)(lpoint.x - rpoint.x);
            int h = (int)(lpoint.y - rpoint.y);

            // if the points are not aligned horizontally
            if (w != 0)
            {
                // choose at random to go horizontal then vertical or the opposite
                if (Random.Range(0, 1) > 2)
                {
                    // add a corridor to the right
                    aisle.Add(new Rect(lpoint.x, lpoint.y, Mathf.Abs(w) + 1, 1));

                    // if left point is below right point go up
                    // otherwise go down
                    if (h < 0)
                        aisle.Add(new Rect(rpoint.x, lpoint.y, 1, Mathf.Abs(h)));
                    else
                        aisle.Add(new Rect(rpoint.x, lpoint.y, 1, -Mathf.Abs(h)));
                }
                else
                {
                    // go up or down
                    if (h < 0)
                        aisle.Add(new Rect(lpoint.x, lpoint.y, 1, Mathf.Abs(h)));
                    else
                        aisle.Add(new Rect(lpoint.x, rpoint.y, 1, Mathf.Abs(h)));

                    // then go right
                    aisle.Add(new Rect(lpoint.x, rpoint.y, Mathf.Abs(w) + 1, 1));
                }
            }
            else
            {
                // if the points are aligned horizontally
                // go up or down depending on the positions
                if (h < 0) aisle.Add(new Rect((int)lpoint.x, (int)lpoint.y, 1, Mathf.Abs(h)));
                else aisle.Add(new Rect((int)rpoint.x, (int)rpoint.y, 1, Mathf.Abs(h)));
            }
        }

        public Rect GetRoom()
        {
            if (IAmLeaf()) return room;
            if (left != null)
            {
                Rect lroom = left.GetRoom();
                if (lroom.x != -1) return lroom;
            }
            if (right != null)
            {
                Rect rroom = right.GetRoom();
                if (rroom.x != -1) return rroom;
            }

            // workaround non nullable structs
            return new Rect(-1, -1, 0, 0);
        }
    }
    public void CreateBSP(SubDungeon subDungeon)
    {
        if (subDungeon.IAmLeaf())
        {
            // if the sub-dungeon is too large split it
            if (subDungeon.rect.width > bsp.maxRoom
                || subDungeon.rect.height > bsp.maxRoom
                || Random.Range(0.0f, 1.0f) > 0.25)
            {
                if (subDungeon.Split(bsp.minRoom, bsp.maxRoom))
                {
                    CreateBSP(subDungeon.left);
                    CreateBSP(subDungeon.right);
                }
            }
        }
    }
    public void DrawRooms(SubDungeon subDungeon)
    {
        if (subDungeon == null) return;

        if (subDungeon.IAmLeaf())
        {
            for (int i = (int)subDungeon.room.x + bsp.offset; i < subDungeon.room.xMax - bsp.offset; i++)
                for (int j = (int)subDungeon.room.y + bsp.offset; j < subDungeon.room.yMax - bsp.offset; j++) grid[i, j] = bsp.room;
        }
        else
        {
            DrawRooms(subDungeon.left);
            DrawRooms(subDungeon.right);
        }
    }

    void Drawaisle(SubDungeon subDungeon)
    {
        if (subDungeon == null) return;

        Drawaisle(subDungeon.left);
        Drawaisle(subDungeon.right);

        foreach (Rect corridor in subDungeon.aisle)
        {
            for (int i = (int)corridor.x; i < corridor.xMax; i++)
                for (int j = (int)corridor.y; j < corridor.yMax; j++)
                    if (grid[i, j] == bsp.wall) grid[i, j] = bsp.aisle;
        }
    }
}
