# API Specification

## Authentication

Authentication is handled using JSON Web Tokens (JWT).

### Register

**POST /api/auth/register**

Creates a new user account.

**Request Body**

```json
{
  "first_name": "name",
  "last_name": "name",
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
  "token": "jwt_token_here"
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
    "comic_id": 1,
    "title": "Batman",
    "issue_number": 1,
    "year_published": 2020,
    "publisher": "DC",
    "status": "available"
  },
  {
    "comic_id": 2,
    "title": "Nightwing",
    "issue_number": 1,
    "year_published": 2020,
    "publisher": "DC",
    "status": "available"
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
  "comic_id": 1
}
```

**Response**
```json
{
  "checkout_id": 10,
  "comic_id": 1,
  "user_id": 5,
  "checkout_date": "2026-03-18",
  "due_date": "2026-04-01",
  "status": "checked_out"
}
```

---

### Return Comic

**PUT /api/checkouts/{id}/return**

**Response** 

```json
{ 
  "message": "Comic returned successfully"
}
```

---

### Get User Checkouts

**GET /api/checkouts/user/{user_id}**

**Authorization:** Bearer Token required

**Response**

```json
{
  "checkout_id": 10,
  "comic_id": 1,
  "checkout_date": "2026-03-01",
  "due_date": "2026-03-15",
  "return_date": null,
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

* Protected endpoints:

    * All POST, PUT, DELETE routes
    * Checkout operations

---

# Notes

* The API is containerized and runs with a PostgreSQL database.

---
