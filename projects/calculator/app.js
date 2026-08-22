const display = document.querySelector("#display");
const expressionDisplay = document.querySelector("#expression");
const keypad = document.querySelector(".keypad");

let expression = "";
let currentInput = "";
let justCalculated = false;
let hasError = false;

function updateDisplay() {
  display.textContent = currentInput || "0";
  expressionDisplay.textContent = expression || "\u00a0";
}

function clearCalculator() {
  expression = "";
  currentInput = "";
  justCalculated = false;
  hasError = false;
  updateDisplay();
}

function enterNumber(number) {
  if (hasError || justCalculated) {
    expression = "";
    currentInput = "";
    justCalculated = false;
    hasError = false;
  }

  if (currentInput === "0") currentInput = "";
  currentInput += number;
  expression += number;
  updateDisplay();
}

function enterDecimal() {
  if (hasError || justCalculated) clearCalculator();
  if (!currentInput) {
    currentInput = "0";
    expression += "0";
  }
  if (!currentInput.includes(".")) {
    currentInput += ".";
    expression += ".";
  }
  updateDisplay();
}

function enterOperation(operation) {
  if (hasError) return;
  const symbol = { add: "+", subtract: "−", multiply: "×", divide: "÷" }[operation];
  const mathSymbol = { add: "+", subtract: "-", multiply: "*", divide: "/" }[operation];

  if (justCalculated) {
    expression = currentInput;
    justCalculated = false;
  }
  if (!currentInput && !expression) return;
  if (!currentInput && /[+\-*/]$/.test(expression)) {
    expression = expression.slice(0, -1) + mathSymbol;
    expressionDisplay.textContent = expression.replaceAll("*", "×").replaceAll("/", "÷");
    return;
  }

  expression += mathSymbol;
  currentInput = "";
  expressionDisplay.textContent = expression.replaceAll("*", "×").replaceAll("/", "÷");
}

function toggleSign() {
  if (hasError) return;
  const oldInput = currentInput;
  const newInput = oldInput.startsWith("-") ? oldInput.slice(1) : `-${oldInput || "0"}`;
  currentInput = newInput;

  if (oldInput) {
    expression = expression.slice(0, -oldInput.length) + newInput;
  } else {
    expression += newInput;
  }
  updateDisplay();
}

function backspace() {
  if (hasError || justCalculated) return;
  if (!currentInput) return;
  currentInput = currentInput.slice(0, -1);
  expression = expression.slice(0, -1);
  updateDisplay();
}

function calculate() {
  if (hasError || !expression || /[+\-*/]$/.test(expression)) return;
  try {
    const completedExpression = expression.replaceAll("*", "×").replaceAll("/", "÷");
    const result = evaluateExpression(expression);
    if (!Number.isFinite(result)) throw new Error("Cannot divide by zero");
    currentInput = formatNumber(result);
    expression = currentInput;
    justCalculated = true;
    updateDisplay();
    expressionDisplay.textContent = `${completedExpression} =`;
  } catch (error) {
    display.textContent = error.message;
    expressionDisplay.textContent = "Try another calculation";
    expression = "";
    currentInput = "";
    hasError = true;
  }
}

function formatNumber(number) {
  return Number(number.toPrecision(12)).toString();
}

// A small two-stack parser gives multiplication/division precedence over +/−.
function evaluateExpression(input) {
  const tokens = input.match(/(?:\d*\.\d+|\d+\.?\d*|[+\-*/])/g) || [];
  if (tokens.join("") !== input) throw new Error("Invalid calculation");
  const values = [];
  const operators = [];
  const precedence = { "+": 1, "-": 1, "*": 2, "/": 2 };

  tokens.forEach((token, index) => {
    if (!Number.isNaN(Number(token)) && token !== "") {
      values.push(Number(token));
      return;
    }

    const unaryMinus = token === "-" && (index === 0 || /[+\-*/]/.test(tokens[index - 1]));
    if (unaryMinus && tokens[index + 1] && !Number.isNaN(Number(tokens[index + 1]))) {
      tokens[index + 1] = `-${tokens[index + 1]}`;
      return;
    }
    while (operators.length && precedence[operators.at(-1)] >= precedence[token]) applyTopOperation(values, operators);
    operators.push(token);
  });

  while (operators.length) applyTopOperation(values, operators);
  if (values.length !== 1) throw new Error("Invalid calculation");
  return values[0];
}

function applyTopOperation(values, operators) {
  const operator = operators.pop();
  const right = values.pop();
  const left = values.pop();
  if (left === undefined || right === undefined) throw new Error("Invalid calculation");
  if (operator === "+") values.push(left + right);
  if (operator === "-") values.push(left - right);
  if (operator === "*") values.push(left * right);
  if (operator === "/") {
    if (right === 0) throw new Error("Cannot divide by zero");
    values.push(left / right);
  }
}

keypad.addEventListener("click", (event) => {
  const button = event.target.closest("button");
  if (!button) return;
  if (button.dataset.number !== undefined) enterNumber(button.dataset.number);
  if (button.dataset.operation) enterOperation(button.dataset.operation);
  if (button.dataset.action === "decimal") enterDecimal();
  if (button.dataset.action === "toggle-sign") toggleSign();
  if (button.dataset.action === "backspace") backspace();
  if (button.dataset.action === "clear") clearCalculator();
  if (button.dataset.action === "equals") calculate();
});

document.addEventListener("keydown", (event) => {
  if (/\d/.test(event.key)) enterNumber(event.key);
  if (event.key === ".") enterDecimal();
  if (["+", "-", "*", "/"].includes(event.key)) enterOperation({ "+": "add", "-": "subtract", "*": "multiply", "/": "divide" }[event.key]);
  if (event.key === "Enter" || event.key === "=") calculate();
  if (event.key === "Backspace") backspace();
  if (event.key === "Escape") clearCalculator();
});

updateDisplay();
