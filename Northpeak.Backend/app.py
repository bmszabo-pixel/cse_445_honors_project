import json
import os
import re
from flask import Flask, jsonify, request
from flask_cors import CORS
from dotenv import load_dotenv
import requests

load_dotenv()

app = Flask(__name__)

# Add CORS to the Flask backend for the Blazor app's origin
CORS(app, resources={r"/api/*": {"origins": "*"}}, supports_credentials=True)

BASE_DIR = os.path.dirname(os.path.abspath(__file__))

def load_json(filename):
    path = os.path.join(BASE_DIR, filename)
    if os.path.exists(path):
        with open(path, "r", encoding="utf-8") as f:
            return json.load(f)
    return []

def load_text(filename):
    path = os.path.join(BASE_DIR, filename)
    if os.path.exists(path):
        with open(path, "r", encoding="utf-8") as f:
            return f.read()
    return ""

products_data = load_json("products.json")
orders_data = load_json("orders.json")
policies_text = load_text("policies.md")

sessions_history = {}

def check_stock_local(product_name):
    query = (product_name or "").strip().lower()
    for p in products_data:
        if p["name"].lower() == query or query in p["name"].lower() or p["id"].lower() == query:
            return {
                "found": True,
                "productId": p["id"],
                "productName": p["name"],
                "stock": p["stock"],
                "inStock": p["stock"] > 0,
                "price": p["price"],
                "message": f"Product '{p['name']}' has {p['stock']} units in stock at ${p['price']:.2f}."
            }
    return {
        "found": False,
        "message": f"Product '{product_name}' was not found in catalog."
    }

def lookup_order_local(order_id):
    query = (order_id or "").strip().upper()
    match = re.search(r"ORD-?\d+", query)
    if match:
        query = match.group(0)
    if re.match(r"^ORD\d+$", query):
        query = "ORD-" + query[3:]
    
    for o in orders_data:
        if o["orderId"].upper() == query:
            return {
                "found": True,
                "orderId": o["orderId"],
                "customerName": o["customerName"],
                "status": o["status"],
                "trackingNumber": o["trackingNumber"],
                "carrier": o["carrier"],
                "eta": o.get("eta", "Pending"),
                "orderDate": o["orderDate"],
                "total": o["total"],
                "items": o["items"],
                "message": f"Order {o['orderId']} for {o['customerName']} is {o['status']} via {o['carrier']}. Tracking: {o['trackingNumber']}."
            }
    return {
        "found": False,
        "message": f"Order '{order_id}' was not found in records."
    }

@app.route("/api/products", methods=["GET"])
def get_products():
    return jsonify(products_data)

@app.route("/api/orders", methods=["GET"])
def get_orders():
    return jsonify(orders_data)

@app.route("/api/orders/<order_id>", methods=["GET"])
def get_order_by_id(order_id):
    result = lookup_order_local(order_id)
    if not result["found"]:
        return jsonify({"error": f"Order {order_id} not found."}), 404
    return jsonify(result)

@app.route("/api/chat", methods=["POST"])
def chat():
    body = request.get_json(force=True, silent=True) or {}
    session_id = body.get("sessionId") or "default_session"
    message = (body.get("message") or "").strip()

    if not message:
        return jsonify({"error": "Message cannot be empty."}), 400

    api_key = os.environ.get("GEMINI_API_KEY")
    if not api_key:
        lower = message.lower()
        tool_calls = []
        if "stock" in lower or "available" in lower:
            stock_res = check_stock_local(message)
            tool_calls.append({
                "toolName": "check_product_stock",
                "argumentsSummary": f"productName = \"{message}\"",
                "resultSummary": f"{stock_res.get('productName', 'Product')}: {stock_res.get('stock', 0)} in stock"
            })
            reply = f"Yes, {stock_res['message']}" if stock_res["found"] else stock_res["message"]
        elif "ord-" in lower or "order" in lower:
            order_res = lookup_order_local(message)
            tool_calls.append({
                "toolName": "lookup_order_status",
                "argumentsSummary": f"orderId = \"{message}\"",
                "resultSummary": f"{order_res.get('orderId', 'Order')}: {order_res.get('status', 'Lookup')}"
            })
            reply = order_res["message"]
        elif "return" in lower or "shipping" in lower or "warranty" in lower:
            reply = "Northwind Gear offers 30-day returns on unused items, free standard shipping on orders over $50 ($6.95 under $50), and a 1-year limited manufacturer warranty against defects."
        elif "python" in lower or "code" in lower or "weather" in lower or "president" in lower:
            reply = "As the Northwind Gear assistant, I can only help with our outdoor equipment products, stock availability, order tracking, and store policies."
        else:
            reply = "Welcome to Northwind Gear! I can help you with product specs, live inventory, order status, and store policies."

        return jsonify({
            "sessionId": session_id,
            "reply": reply,
            "toolCalls": tool_calls,
            "success": True,
            "error": None
        })

    model_name = os.environ.get("GEMINI_MODEL", "gemini-3.1-flash-lite")
    history = sessions_history.setdefault(session_id, [])

    history.append({
        "role": "user",
        "parts": [{"text": message}]
    })

    system_instruction = f"""
You are the official AI Sales and Customer Support Chatbot for "Northwind Gear" (outdoor equipment and apparel).
=== STORE POLICIES ===
{policies_text}
=== RULES ===
1. Answer product questions using the Northwind Gear catalog only.
2. For stock inquiries, call check_product_stock.
3. For order tracking, call lookup_order_status.
4. For store policies (30-day returns, free shipping over $50, 1-year warranty), use the policies text.
5. If outside scope, politely say you can only help with Northwind Gear questions.
"""

    tools = [{
        "functionDeclarations": [
            {
                "name": "check_product_stock",
                "description": "Checks stock availability for a product by name",
                "parameters": {
                    "type": "OBJECT",
                    "properties": {
                        "productName": {"type": "STRING", "description": "The product name to check"}
                    },
                    "required": ["productName"]
                }
            },
            {
                "name": "lookup_order_status",
                "description": "Looks up order status and tracking by order ID",
                "parameters": {
                    "type": "OBJECT",
                    "properties": {
                        "orderId": {"type": "STRING", "description": "The order ID such as ORD-1001"}
                    },
                    "required": ["orderId"]
                }
            }
        ]
    }]

    tool_calls_recorded = []

    try:
        url = f"https://generativelanguage.googleapis.com/v1beta/models/{model_name}:generateContent?key={api_key}"
        payload = {
            "contents": history,
            "tools": tools,
            "systemInstruction": {"parts": [{"text": system_instruction}]}
        }

        resp = requests.post(url, json=payload, timeout=12)
        if not resp.ok:
            return jsonify({"reply": "Trouble communicating with Gemini service.", "toolCalls": [], "success": False, "error": resp.text}), 500

        data = resp.json()
        candidates = data.get("candidates", [])
        if not candidates:
            return jsonify({"reply": "No response generated.", "toolCalls": [], "success": True})

        cand = candidates[0].get("content", {})
        parts = cand.get("parts", [])

        function_call = next((p.get("functionCall") for p in parts if "functionCall" in p), None)

        if not function_call:
            reply_text = "".join(p.get("text", "") for p in parts)
            history.append(cand)
            return jsonify({
                "sessionId": session_id,
                "reply": reply_text,
                "toolCalls": tool_calls_recorded,
                "success": True,
                "error": None
            })

        # Append model's turn
        history.append(cand)

        func_name = function_call.get("name")
        func_args = function_call.get("args", {})
        func_id = function_call.get("id")

        if func_name == "check_product_stock":
            p_name = func_args.get("productName", "")
            tool_res = check_stock_local(p_name)
            tool_calls_recorded.append({
                "toolName": "check_product_stock",
                "argumentsSummary": f"productName = \"{p_name}\"",
                "resultSummary": f"{tool_res.get('productName', 'Product')}: {tool_res.get('stock', 0)} in stock"
            })
        elif func_name == "lookup_order_status":
            o_id = func_args.get("orderId", "")
            tool_res = lookup_order_local(o_id)
            tool_calls_recorded.append({
                "toolName": "lookup_order_status",
                "argumentsSummary": f"orderId = \"{o_id}\"",
                "resultSummary": f"{tool_res.get('orderId', 'Order')}: {tool_res.get('status', 'Lookup')}"
            })
        else:
            tool_res = {"error": "Unknown tool"}

        func_part = {
            "functionResponse": {
                "name": func_name,
                "response": tool_res
            }
        }
        if func_id:
            func_part["functionResponse"]["id"] = func_id

        history.append({
            "role": "function",
            "parts": [func_part]
        })

        # Step 2: Call Gemini again to get natural synthesis
        resp2 = requests.post(url, json={"contents": history, "tools": tools}, timeout=12)
        if resp2.ok:
            data2 = resp2.json()
            cand2 = data2.get("candidates", [])[0].get("content", {})
            reply_text = "".join(p.get("text", "") for p in cand2.get("parts", []))
            history.append(cand2)
        else:
            reply_text = tool_res.get("message", "Request completed.")

        return jsonify({
            "sessionId": session_id,
            "reply": reply_text,
            "toolCalls": tool_calls_recorded,
            "success": True,
            "error": None
        })

    except Exception as e:
        return jsonify({"reply": f"An error occurred: {str(e)}", "toolCalls": [], "success": False, "error": str(e)}), 500

@app.route("/api/status", methods=["GET"])
def get_status():
    return jsonify({
        "status": "online",
        "products": len(products_data),
        "orders": len(orders_data),
        "hasKey": bool(os.environ.get("GEMINI_API_KEY"))
    })

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000, debug=False)
