# 🛒 TCG Fulfillment Engine API

A high-performance order processing engine built with **ASP.NET Core Minimal APIs** and **Entity Framework Core**. This system simulates the backend of a Trading Card Game (TCG) store, managing real-time inventory allocation under high concurrency.

The project focuses on solving complex relational database challenges—such as race conditions, thread deadlocks, and performance bottlenecks—using advanced C# and EF Core techniques.

## ✨ Core Features (Architecture)

* **Concurrent Processing (Thread-Safety):** Leverages `Task.WhenAll` and `IDbContextFactory` to isolate threads, allowing multiple orders to be processed simultaneously without sharing database context instances.
* **Optimistic Concurrency Control:** Handles inventory read/write collisions using a custom retry mechanism with a randomized jitter delay (10ms-40ms), allowing row locks to clear gracefully and preventing failed transactions.
* **Priority Scheduling (Two-Lane Isolation):** Orders marked with `SpeedPlus` priority are processed in a parallel fast lane (Lane A) before standard orders (Lane B) are released for processing.
* **Global Exception Handling:** Implements `IExceptionHandler` to safely catch fatal errors, preventing raw stack trace leaks to clients and converting client disconnects into controlled telemetry warnings.
* **Safe & Additive Seeding:** The `/Seed` endpoint is designed to populate the database by dynamically mapping quantities to SKUs, guaranteeing that historical records are preserved and foreign key constraints are strictly respected.

## 🛠️ Tech Stack

* **Framework:** .NET 8 / ASP.NET Core (Minimal APIs)
* **Database:** SQL Server & Entity Framework Core
* **Logging & Telemetry:** Serilog (with semantic severity levels)
* **Design Patterns:** Factory Pattern, Optimistic Concurrency, Global Exception Handling.

## 🚀 API Endpoints

### 1. `POST /Burst`
Triggers the order processing engine. Evaluates order lines against available inventory, updates the state to `Fulfilled` or `Backordered`, and applies priority routing (SpeedPlus vs. Normal).

### 2. `POST /Benchmark?n={quantity}`
Executes a synchronous performance benchmark. It processes a batch of orders sequentially, resets the state, and then processes them in parallel to calculate the **Speedup Factor** of the concurrent architecture.
* **Usage Example:** `curl -X POST "http://localhost:5000/Benchmark?n=150" -H "Content-Length: 0"`

### 3. `GET /reports/top-products`
Generates a real-time analytical report that groups, sums, and sorts the highest volume products allocated in the database, using highly optimized SQL queries.
* **Usage Example:** `curl -X GET "http://localhost:5000/reports/top-products"`

### 4. `POST /Seed`
Restores or appends the baseline inventory without destroying historical transactional records. It automatically handles SKU mapping to prevent Primary Key or Foreign Key constraint violations.

## 📊 Database Structure

* **Cards:** The product catalog (e.g., SKUs like `YGO-GAOV-EN032-UR`).
* **Inventories:** Tracks real-time stock levels (`QuantityOnHand`).
* **Orders & OrderLines:** Header and detail structure for purchase orders.
* **FulfillmentLogs:** An immutable audit log that records every allocation attempt, concurrency collision, and system error.