# Crypto Price Tracker – Full Stack Developer Challenge

## 📋 Objective

This challenge is designed to evaluate your skills as a full stack developer using .NET 6 (C#), Entity Framework Core, Razor Pages, and REST API integration.

You'll be working with a project that simulates tracking cryptocurrency prices using the CoinGecko API.

---

## 🧠 What You Need to Do

### ✅ 1. Fix the Service Logic

- Open `CryptoPriceService.cs` and fix the logic so it correctly:
  - Fetches coin data from CoinGecko for all existing coins  
    Documentation: https://docs.coingecko.com/v3.0.1/reference/introduction
  - Handles async operations properly
  - Prevents duplicate entries
  - Saves the results to the SQLite database

  ⚠️ If you encounter duplicate entries or errors from the CoinGecko API, decide how to handle them.  
  You're free to choose the best approach — just make sure you explain your reasoning in a short code comment.

  ⚠️ Some files may have outdated or incorrect `namespace` declarations. Please review and update them as needed to ensure consistency across the project.

### ✅ 2. Complete the Controller

- Open `CryptoController.cs` and:
  - Complete the `POST /api/crypto/update-prices` endpoint
  - Add a `GET /api/crypto/latest-prices` endpoint to return the most recent price per asset. All available cryptocoins must be saved.

### ✅ 3. Test the Razor Frontend

- The view in `/Views/Home/Index.cshtml` includes a button to call the API.
- Ensure that the interface provides clear feedback to the user indicating whether the update succeeded or failed.

### ✅ 4. Create the Frontend Visualization in `Index.cshtml`

Build a clean and user-friendly visualization directly in the existing Razor view:  
**`/Views/Home/Index.cshtml`**

This page must:

- Display **each cryptocurrency asset** with at least the following fields:
  - ✅ **Name**
  - ✅ **Symbol**
  - ✅ **Current Price**
  - ✅ **Currency**
  - ✅ **Icon**
    - Add the `IconUrl` property to the `CryptoAsset` model
    - Retrieve the image url from CoinGecko
    - Save it to the database
  - ✅ **Last Updated**
    - This value must be converted to the client's local timezone using JavaScript
  - ✅ **Trend**
    - Add a visual indicator showing whether the price has increased, decreased, or stayed the same compared to the previous saved price (e.g., 🔼, 🔽, ➖)
    - Optionally, include the percentage change

- Use the existing "Update Prices" button to trigger the API and refresh the data displayed in the view.

> 🎯 **Goal:** The user should be able to open the page, see the most recent cryptocurrency data clearly presented, and update it with one click.

> 💡 Bonus points for improved UI, animations, or anything that enhances clarity and usability.

### ✅ 5. Add Improvements

- Add validation logic or error handling where needed
- Create unit tests for validator or service logic

---

## ⚙️ How to Run the Project

1. Make sure you have **.NET 6 SDK** installed.
2. Navigate to the backend project folder:
   ```bash
   cd .../CryptoPriceTracker.Api
   ```
3. Restore dependencies:
   ```bash
   dotnet restore
   ```
4. Generate your own migrations and update the database:
   ```bash
   dotnet ef migrations add InitialCreate --project "CryptoPriceTracker.Infrastructure" --startup-project "CryptoPriceTracker.Api"
   dotnet ef database update --project "CryptoPriceTracker.Infrastructure" --startup-project "CryptoPriceTracker.Api"
   ```
5. Update the env variables to add the CoinGecko Api Key
6. Run the project:
   ```bash
   dotnet run
   ```
7. Navigate to `http://localhost:5000` to test the Razor page.

---

## 🧪 Testing

You may add unit tests to demonstrate how you validate business logic (e.g., in the `PriceValidator` class).

---

## 💬 Assumptions and Decisions

✏️ If you made any assumptions due to missing or unclear requirements, please **leave a brief comment** in the code explaining what you assumed and why.

---

## 🧠 Optional Architecture Upgrade

💡 You may reorganize the project structure or apply your preferred architecture (e.g., Clean Architecture, layered services, etc.)  
as long as:

- The functionality remains the same  
- The Razor page and API endpoints still work as requested  
- You document your reasoning in code comments or a note in the README

---

## 📝 Notes

- 🔸 **Do not include generated migrations or database files in your submission.**
- The project uses SQLite via EF Core for simplicity.
- CoinGecko API is public and doesn't require authentication for basic price lookups.
- The crypto icon should be retrieved from the CoinGecko API.

---

## 🧾 Deliverables Checklist

Before submitting your solution, please ensure the following items are completed:

- [ ] `CryptoPriceService.cs` fetches, saves, and avoids duplicates correctly
- [ ] `namespace` declarations are consistent and correct across all files
- [ ] `POST /api/crypto/update-prices` endpoint is functional
- [ ] `GET /api/crypto/latest-prices` endpoint returns most recent prices
- [ ] `IconUrl` property added to `CryptoAsset` model and stored in DB
- [ ] Razor page (`Index.cshtml`) includes:
  - [ ] Name
  - [ ] Symbol
  - [ ] Current Price and currency
  - [ ] Icon
  - [ ] Last Updated (adjusted to client timezone)
  - [ ] Trend (up/down indicator and optional percentage change)
- [ ] Button updates data and refreshes the view
- [ ] Clear UI feedback after update attempts (success/failure)
- [ ] Validation/error handling is implemented where needed
- [ ] At least one unit test demonstrates validation or service logic
- [ ] Comments provided for any assumptions or technical decisions

---

Good luck, and thank you for taking the time to complete this challenge!

# Important Notes
- Implemented Task to manage parallel request to coingecko api, this to avoid waiting to much time
- As there were many request to the API it was decided to make a sort by last update to get the coins -without any update in our system in the last 30 min
- it was assumed that it is necessary to take all the coins on the API coin gecko
- It was required to get the coingecko api key to get the assets' icon so please check the shared file or send me an email to get the key or you can use yours