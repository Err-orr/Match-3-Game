using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Dot : MonoBehaviour
{
    [Header("Board Variables")]
    public int column; // Current column of the dot
    public int row; // Current row of the dot
    public int previousColumn; // Previous column (for reverting moves)
    public int previousRow; // Previous row (for reverting moves)
    public int targetX; // Target X position for movement
    public int targetY; // Target Y position for movement
    public bool isMatched = false; // Flag for matching status
    private Board board; // Reference to the board
    public GameObject otherDot; // Reference to the dot being swapped with
    private Vector2 firstTouchPosition; // Position of the first touch
    private Vector2 finalTouchPosition; // Position of the final touch
    private Vector2 tempPosition; // Temporary position for movement
    public float swipeAngle = 0; // Angle of the swipe
    public float swipeResist = 1f; // Resistance threshold for swipe detection
    private FindMatches findMatches; // Reference to the FindMatches script
    [Header("Power Up Variables")]
    public bool isColorBomb;
    public bool isColumnBomb; // Indicates if this dot is a column bomb
    public bool isRowBomb; // Indicates if this dot is a row bomb
    public GameObject rowArrow; // Prefab for the row arrow visual
    public GameObject columnArrow; // Prefab for the column arrow visual
    public GameObject colorBomb;

    // Start is called before the first frame update
    void Start()
    {
        isColumnBomb = false;
        isRowBomb = false;
        board = FindObjectOfType<Board>(); // Find and reference the Board
        findMatches = FindObjectOfType<FindMatches>(); // Reference to FindMatches
        // Uncomment to initialize target positions and row/column
        /* targetX = (int)transform.position.x; 
        targetY = (int)transform.position.y;
        row = targetY;
        column = targetX;
        previousRow = row;
        previousColumn = column; */
    }
    // Debug method.
    private void OnMouseOver() {
        if (Input.GetMouseButtonDown(1)) {
            isColorBomb = true; // Set column bomb flag
            GameObject color = Instantiate(colorBomb, transform.position, Quaternion.identity);
            color.transform.parent = this.transform; // Set arrow as child of this dot
        }
    }
    // Update is called once per frame
    void Update()
    {
        /* If the dot is matched, change its color for visual feedback
        if (isMatched) {
            SpriteRenderer mySprite = GetComponent<SpriteRenderer>();
            mySprite.color = new Color(0f, 0f, 0f, .2f); // Change color for matched dots
        }

        Update target position based on current column and row */
        targetX = column;
        targetY = row;

        // Move towards target X position
        if (Mathf.Abs(targetX - transform.position.x) > .1) {
            tempPosition = new Vector2(targetX, transform.position.y); // Set new position
            transform.position = Vector2.Lerp(transform.position, tempPosition, .1f); // Smooth movement
            // Update board reference if this dot is in the new position
            if (board.allDots[column, row] != this.gameObject) {
                board.allDots[column, row] = this.gameObject;
            }
            findMatches.FindAllMatches(); // Check for matches
        } else {
            // Directly set position if close enough
            tempPosition = new Vector2(targetX, transform.position.y);
            transform.position = tempPosition; // Set to target position
            board.allDots[column, row] = this.gameObject;  // Update board reference
        }

        // Move towards target Y position
        if (Mathf.Abs(targetY - transform.position.y) > .1) {
            tempPosition = new Vector2(transform.position.x, targetY); // Set new position
            transform.position = Vector2.Lerp(transform.position, tempPosition, .1f); // Smooth movement
            // Update board reference if this dot is in the new position
            if (board.allDots[column, row] != this.gameObject) {
                board.allDots[column, row] = this.gameObject;
            }
            findMatches.FindAllMatches(); // Check for matches
        } else {
            // Directly set position if close enough
            tempPosition = new Vector2(transform.position.x, targetY);
            transform.position = tempPosition; // Set to target position
            board.allDots[column, row] = this.gameObject; // Update board reference
        }
    }

    // Coroutine to check the validity of the move
    public IEnumerator CheckMoveCo() {
        if (isColorBomb) {
            // This piece is a color bomb, and the other piece is the color to destroy.
            findMatches.MatchPiecesOfColor(otherDot.tag);
            isMatched = true;
        } else if (otherDot.GetComponent<Dot>().isColorBomb){
            // The other piece is a color bomb, and this piece is the color to destroy.
            findMatches.MatchPiecesOfColor(this.gameObject.tag);
            otherDot.GetComponent<Dot>().isMatched = true;
        }
        yield return new WaitForSeconds(.1f); // Wait before checking
        if (otherDot != null) {
            if (!isMatched && !otherDot.GetComponent<Dot>().isMatched) {
                // If no match, revert the move
                otherDot.GetComponent<Dot>().row = row; // Revert other dot's position
                otherDot.GetComponent<Dot>().column = column; // Revert other dot's column
                row = previousRow; // Restore previous row
                column = previousColumn; // Restore previous column
                yield return new WaitForSeconds(.2f); // Wait for destruction to complete
                board.currentDot = null;
                board.currentState = GameState.move; // Allow moving again
            } else {
                // If there is a match, destroy matched dots
                board.DestroyMatches(); // Trigger match destruction
            }
            // otherDot = null; // Reset otherDot reference
        }
    }

    // Handle mouse down event for touch input
    private void OnMouseDown() {
        if (board.currentState == GameState.move) {
            firstTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition); // Get initial touch position
        }
    }

    // Handle mouse up event for touch input
    private void OnMouseUp() {
        if (board.currentState == GameState.move) {
            finalTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition); // Get final touch position
            CalculateAngle(); // Calculate swipe angle and direction
        }
    }

    // Calculate the angle of the swipe
    void CalculateAngle() {
        // Calculate the angle of the swipe based on touch movement
        if(Mathf.Abs(finalTouchPosition.y - firstTouchPosition.y) > swipeResist || Mathf.Abs(finalTouchPosition.x - firstTouchPosition.x) > swipeResist) {
            // Calculate the angle in degrees
            swipeAngle = Mathf.Atan2(finalTouchPosition.y - firstTouchPosition.y, finalTouchPosition.x - firstTouchPosition.x) * 180 / Mathf.PI;
            Debug.Log(swipeAngle); // Log the angle for debugging
            MovePieces(); // Move the pieces based on swipe direction
            board.currentState = GameState.wait; // Change state to wait
            board.currentDot = this;
        } else {
            board.currentState = GameState.move; // Reset state if swipe is too short
        }
    }
    // Move the pieces based on the swipe direction calculated
    void MovePieces() {
        // Determine the direction of the swipe and move the dots accordingly
        if(swipeAngle > -45 && swipeAngle <= 45 && column < board.width - 1) {
            // Right Swipe
            otherDot = board.allDots[column + 1, row]; // Get the dot to the right
            previousRow = row; // Save previous row
            previousColumn = column; // Save previous column
            otherDot.GetComponent<Dot>().column -= 1; // Move that dot left
            column += 1; // Update current dot's column
        } else if (swipeAngle > 45 && swipeAngle <= 135 && row < board.height - 1) {
            // Up Swipe
            otherDot = board.allDots[column, row + 1]; // Get the dot above
            previousRow = row; // Save previous row
            previousColumn = column; // Save previous column
            otherDot.GetComponent<Dot>().row -= 1; // Move that dot down
            row += 1; // Update current dot's row
        } else if ((swipeAngle > 135 || swipeAngle <= -135) && column > 0) {
            // Left Swipe
            otherDot = board.allDots[column - 1, row]; // Get the dot to the left
            previousRow = row; // Save previous row
            previousColumn = column; // Save previous column
            otherDot.GetComponent<Dot>().column += 1; // Move that dot right
            column -= 1; // Update current dot's column
        } else if (swipeAngle < -45 && swipeAngle >= -135 && row > 0) {
            // Down Swipe
            otherDot = board.allDots[column, row - 1]; // Get the dot below
            previousRow = row; // Save previous row
            previousColumn = column; // Save previous column
            otherDot.GetComponent<Dot>().row += 1; // Move that dot up
            row -= 1; // Update current dot's row
        }
        StartCoroutine(CheckMoveCo()); // Start the coroutine to check the move validity
    }
    // Find matches with adjacent dots
    void FindMatches() {
        // Check for matches with adjacent dots
        if (column > 0 && column < board.width - 1) {
            GameObject leftDot1 = board.allDots[column - 1, row]; // Dot to the left
            GameObject rightDot1 = board.allDots[column + 1, row]; // Dot to the right
            if (leftDot1 != null && rightDot1 != null) {
                // Check if both adjacent dots match the current dot's tag
                if (leftDot1.tag == this.gameObject.tag && rightDot1.tag == this.gameObject.tag) {
                    leftDot1.GetComponent<Dot>().isMatched = true; // Mark left dot as matched
                    rightDot1.GetComponent<Dot>().isMatched = true; // Mark right dot as matched
                    isMatched = true; // Mark current dot as matched
                }
            }
        }
        // Check for matches with dots above and below
        if (row > 0 && row < board.height - 1) {
            GameObject upDot1 = board.allDots[column, row + 1]; // Dot above
            GameObject downDot1 = board.allDots[column, row - 1]; // Dot below
            if (upDot1 != null && downDot1 != null) {
                // Check if both adjacent dots match the current dot's tag
                if (upDot1.tag == this.gameObject.tag && downDot1.tag == this.gameObject.tag) {
                    upDot1.GetComponent<Dot>().isMatched = true; // Mark upper dot as matched
                    downDot1.GetComponent<Dot>().isMatched = true; // Mark lower dot as matched
                    isMatched = true; // Mark current dot as matched
                }
            }
        }
    }

    public void MakeRowBomb() {
        isRowBomb = true; // Set row bomb flag
        GameObject arrow = Instantiate(rowArrow, transform.position, Quaternion.identity);
        arrow.transform.parent = this.transform; // Set arrow as child of this dot
    }

    public void MakeColumnBomb() {
        isColumnBomb = true; // Set column bomb flag
        GameObject arrow = Instantiate(columnArrow, transform.position, Quaternion.identity);
        arrow.transform.parent = this.transform; // Set arrow as child of this dot
    }
}