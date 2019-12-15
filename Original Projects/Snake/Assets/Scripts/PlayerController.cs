﻿using System;
using System.Collections;
using System.Collections.Generic;
using Snake.Grid;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public event Action<GridCell> playerMovementEvent;
    public event Action ateOwnTailEvent;
    public float cellsPerSecondMovement;
    public GameObject snakeCellPrefab;

    private GridController gridController;
    private ScoreController scoreController;
    private GameController gameController;
    private PlayerMovementController playerMovementController;

    void Start()
    {
        gridController = FindObjectOfType<GridController>();
        scoreController = FindObjectOfType<ScoreController>();
        gameController = FindObjectOfType<GameController>();
        playerMovementController = new PlayerMovementController(this, cellsPerSecondMovement);
        gameController.resetGameEvent += reset;
        scoreController.scoreEvent += onScoreEvent;
    }

    void Update()
    {
        playerMovementController.run();
    }

    private void onScoreEvent(int aNewScore)
    {
        playerMovementController.incrementTail();
    }

    private void reset()
    {
        playerMovementController.reset();
    }

    private class PlayerMovementController
    {
        private Direction prevDirection;
        private Direction nextDirection;
        private PlayerController playerController;
        private float cellsPerSecond;
        private float movementWaitTime;
        private float timeUntilNextMove;
        private LinkedList<SnakeCell> snakeCells;

        internal PlayerMovementController(PlayerController playerController, float cellsPerSecond) {
            this.playerController = playerController;
            this.cellsPerSecond = cellsPerSecond;
            this.movementWaitTime = 1 / cellsPerSecond;
            this.timeUntilNextMove = movementWaitTime;
            prevDirection = Direction.LEFT;
            nextDirection = Direction.RIGHT;
            snakeCells = new LinkedList<SnakeCell>();
            GridCell startingCell = playerController.gridController.getCenter();
            snakeCells.AddFirst(new SnakeCell(this, startingCell));
        }

        internal void reset()
        {
            foreach (SnakeCell snakeCell in snakeCells) {
                Destroy(snakeCell.gameObject);
            }
            nextDirection = Direction.RIGHT;
            snakeCells = new LinkedList<SnakeCell>();
            GridCell startingCell = playerController.gridController.getCenter();
            snakeCells.AddFirst(new SnakeCell(this, startingCell));
        }

        internal void run()
        {
            updateInputDirection();
            checkMovement();
            checkIfEatingOwnTail();
        }

        internal void incrementTail()
        {
            snakeCells.AddLast(new SnakeCell(this, snakeCells.Last.Value.pos));
        }

        private void updateInputDirection()
        {
            if (playerPressedUp() && prevDirection != Direction.DOWN) {
                nextDirection = Direction.UP;
            } else if (playerPressedDown() && prevDirection != Direction.UP) {
                nextDirection = Direction.DOWN;
            } else if (playerPressedLeft() && prevDirection != Direction.RIGHT) {
                nextDirection = Direction.LEFT;
            } else if (playerPressedRight() && prevDirection != Direction.LEFT) {
                nextDirection = Direction.RIGHT;
            }
        }

        private bool playerPressedUp()
        {
            return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
        }

        private bool playerPressedDown()
        {
            return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
        }

        private bool playerPressedLeft()
        {
            return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
        }

        private bool playerPressedRight()
        {
            return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
        }

        private void checkMovement()
        {
            timeUntilNextMove -= Time.deltaTime;
            if (timeUntilNextMove < 0) {
                move();
                timeUntilNextMove = movementWaitTime;
            }
        }

        private void move()
        {
            GridCell prevHeadPos = snakeCells.First.Value.pos;
            GridCell nextHeadPos = prevHeadPos;
            switch (nextDirection) {
                case Direction.UP: {
                    nextHeadPos = GridCell.of(prevHeadPos.gridPos.x, prevHeadPos.gridPos.y + 1);
                    break;
                }
                case Direction.DOWN: {
                    nextHeadPos = GridCell.of(prevHeadPos.gridPos.x, prevHeadPos.gridPos.y - 1);
                    break;
                }
                case Direction.LEFT: {
                    nextHeadPos = GridCell.of(prevHeadPos.gridPos.x - 1, prevHeadPos.gridPos.y);
                    break;
                }
                case Direction.RIGHT: {
                    nextHeadPos = GridCell.of(prevHeadPos.gridPos.x + 1, prevHeadPos.gridPos.y);
                    break;
                }
                default: {
                    throw new InvalidOperationException("Enum not handled: " + nextDirection);
                };
            }

            prevDirection = nextDirection;
            SnakeCell last = snakeCells.Last.Value;
            snakeCells.RemoveLast();
            snakeCells.AddFirst(last);
            last.pos = nextHeadPos;
            playerController.gridController.placeInCell(last.gameObject, nextHeadPos);

            if (playerController.playerMovementEvent != null) {
                playerController.playerMovementEvent(nextHeadPos);
            }
        }

        private void checkIfEatingOwnTail()
        {
            if (!canEatOwnTail()) {
                return;
            }

            SnakeCell head = snakeCells.First.Value;
            LinkedListNode<SnakeCell> currSnakeCell = snakeCells.First;
            while (currSnakeCell.Next != null) {
                currSnakeCell = currSnakeCell.Next;
                if (currSnakeCell.Value.pos.Equals(head.pos)) {
                    playerController.ateOwnTailEvent();
                    return;
                }
            }
        }

        private bool canEatOwnTail()
        {
            return snakeCells.Count > 4;
        }

        private class SnakeCell {
            internal GridCell pos;
            internal readonly GameObject gameObject;

            internal SnakeCell(PlayerMovementController playerMovementController, GridCell pos) {
                this.pos = pos;
                gameObject = Instantiate(playerMovementController.playerController.snakeCellPrefab);
                gameObject.transform.parent = playerMovementController.playerController.transform;
                playerMovementController.playerController.gridController.placeInCell(gameObject, pos);
            }
        }

        private enum Direction
        {
            UP,
            DOWN,
            LEFT,
            RIGHT
        }
    }
}