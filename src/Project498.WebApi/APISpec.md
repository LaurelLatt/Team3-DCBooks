# API Specification

## Authentication

Authentication is handled using JSON Web Tokens (JWT).

### Register

**POST /api/auth/register**

Creates a new user account.

**Request Body**

```json
{
  "firstName": "name",
  "lastName": "name",
  "username": "username",
  "email": "user@email.com",
  "password": "password123"
}
```

**Response**

```json
{
  "message": "User registered successfully"
}
```

**Error Response (example)**

```json
{
  "code": "USERNAME_EXISTS",
  "message": "Username already exists."
}
```

Username and email are unique at both the application and database levels. Duplicate values return `409 Conflict`.

---

### Login

**POST /api/auth/login**

Authenticates a user and returns a Bearer token.

**Request Body**

```json
{
  "username": "username",
  "password": "password123"
}
```

**Response**

```json
{
  "access_token": "jwt_token_here",
  "token": "jwt_token_here"
}
```

**Error Response (example)**

```json
{
  "code": "INVALID_CREDENTIALS",
  "message": "Invalid username or password."
}
```

---

## Comics

### Get All Comics

**GET /api/comics**

Returns a list of all comics.

**Response**

```json
[
  {
    "comicId": 1,
    "title": "Batman",
    "issueNumber": 1,
    "yearPublished": 2020,
    "publisher": "DC",
    "status": "available",
    "checkedOutBy": null,
    "characterIds": [1, 4],
    "characterNames": ["Batman", "The Flash"]
  },
  {
    "comicId": 2,
    "title": "Nightwing",
    "issueNumber": 1,
    "yearPublished": 2020,
    "publisher": "DC",
    "status": "available",
    "checkedOutBy": null,
    "characterIds": [2],
    "characterNames": ["Superman"]
  }
]
```

---

### Get Comic by ID

**GET /api/comics/{id}**

Returns a single comic.

### Get Comics (with optional filters)
GET /api/comics

Returns a list of comics. Supports optional query parameters for filtering.

#### Query Parameters (optional)
- title
- issue_number
- year_published
- publisher
- character

#### Example Requests
GET /api/comics?title=Batman  
GET /api/comics?character=Joker  
GET /api/comics?publisher=DC&year_published=2020

### Create Comic (Protected)

**POST /api/comics**

**Authorization:** Bearer Token required

---

### Update Comic (Protected)

**PUT /api/comics/{id}**

---

### Delete Comic (Protected)

**DELETE /api/comics/{id}**

---

## Characters

### Get All Characters

**GET /api/characters**

---

### Get Character by ID

**GET /api/characters/{id}**

---

## Checkouts

### Checkout Comic

**POST /api/checkouts**

**Authorization:** Bearer Token required

**Request Body**

```json
{
  "comicId": 1
}
```

**Response**
```json
{
  "checkoutId": 10,
  "comicId": 1,
  "userId": 5,
  "checkoutDate": "2026-03-18T00:00:00Z",
  "dueDate": "2026-04-01T00:00:00Z",
  "status": "checked_out"
}
```

---

### Return Comic

**PUT /api/checkouts/{id}/return**

Compatibility route also supported:

**PUT /api/checkouts/{id}**

**Response** 

```json
{
  "checkoutId": 10,
  "comicId": 1,
  "userId": 5,
  "checkoutDate": "2026-03-18T00:00:00Z",
  "dueDate": "2026-04-01T00:00:00Z",
  "returnDate": "2026-03-25T00:00:00Z",
  "status": "returned"
}
```

---

### Get User Checkouts

**GET /api/checkouts/user/{userId}**

**Response**

```json
{
  "checkoutId": 10,
  "comicId": 1,
  "checkoutDate": "2026-03-01T00:00:00Z",
  "dueDate": "2026-03-15T00:00:00Z",
  "returnDate": null,
  "status": "checked_out"
}
```
---

# Data Models

## Users

* user_id (PK)
* first_name
* last_name
* username
* email
* password

## Comics

* comic_id (PK)
* title
* issue_number
* year_published
* publisher
* status
* checked_out_by (FK)

## Characters

* character_id (PK)
* name
* alias
* description

## Comic_Characters

* comic_id (PK, FK)
* character_id (PK, FK)

## Checkouts

* checkout_id (PK)
* user_id (FK)
* comic_id (FK)
* checkout_date
* due_date
* return_date
* status

# Authorization Rules

* Public endpoints:

    * GET /api/comics
    * GET /api/comics/{id}
    * GET /api/characters
    * GET /api/characters/{id}
    * GET /api/checkouts
    * GET /api/checkouts/{id}
    * GET /api/checkouts/user/{userId}

* Protected endpoints:

    * POST /api/comics
    * PUT /api/comics/{id}
    * DELETE /api/comics/{id}
    * POST /api/characters
    * PUT /api/characters/{id}
    * DELETE /api/characters/{id}
    * POST /api/checkouts
    * PUT /api/checkouts/{id}
    * PUT /api/checkouts/{id}/return

---

# Error Format

All API validation/domain errors return this JSON shape:

```json
{
  "code": "ERROR_CODE",
  "message": "Human readable message."
}
```

---

# Notes

* The API is containerized and runs with a PostgreSQL database.

---
