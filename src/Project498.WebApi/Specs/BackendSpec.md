# Backend Specification

## Overview

The backend is responsible for handling user authentication, managing sessions, and communicating with the Comic Library API. It acts as an intermediary between the frontend and the API.

---

## Responsibilities

* Handle user login and registration
* Store and manage JWT tokens
* Send requests to the API
* Process API responses and return data to frontend

---

## Authentication

### Login

* Receives username and password from frontend
* Sends request to API (`POST /api/auth/login`)
* Stores returned JWT token in session

### Register

* Sends user data to API (`POST /api/auth/register`)
* Returns success/failure response to frontend

---

## API Communication Examples

### Get Comics

* Calls `GET /api/comics`
* Returns data to frontend

### Get Comic Details

* Calls `GET /api/comics/{id}`

### Checkout Comic

* Calls `POST /api/checkouts`
* Includes Bearer token in request header

### Get User Checkouts

* Calls `GET /api/checkouts/{id}`
* Includes Bearer token

---

## Data Handling

* Backend does not store persistent data (handled by API)
* May temporarily store session/token information

---

## Error Handling

* Handles API errors and returns user-friendly messages
* Ensures unauthorized requests redirect to login page

---

## Security

* Uses Basic Authentication for website login handling
* Stores JWT securely (session or HTTP-only cookies)

---
