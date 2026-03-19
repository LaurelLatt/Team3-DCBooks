## Feature 1: User Registration and Login

### Overview / Purpose

This feature allows new users to register an account and existing users to log in. 
It ensures secure authentication so that users can access protected parts of the website, such as borrowing comics or viewing their current checkouts.

### Scope

* Includes user registration, login, and logout
* Handles validation of required fields, uniqueness of username/email
* Uses JWT Bearer token for session management
* Does not include password reset or multi-factor authentication

### Functional Requirements

1. **Registration**

    * User provides first name, last name, username, email, and password
    * System validates input fields
      * email must be in valid format
      * password must meet minimum length requirements
      * username must not be empty and within length limits
      * required fields cannot be blank
      * leading/trailing spaces are trimmed
    * System checks that username/email is unique
    * System creates a new user account in the database
    * System displays a confirmation message and redirects to login page

2. **Login**

    * User provides username and password
    * System validates credentials via API (`POST /api/auth/login`)
    * System returns JWT token on successful login
    * System stores JWT token in session/local storage
    * User is redirected to homepage

3. **Error Handling**

    * Invalid input → show error message
    * Username/email already exists → show error message
    * Incorrect credentials → show error message

4**Logout**

   * User can click Logout from User Info page
   * System clears data
   * User would be redirected to login or home page
   * protected pages become inaccessible until user logs in again

---

## Feature 2: Browse and Search Comics

### Overview / Purpose

This feature allows users to browse the comic library and search/filter comics by various characteristics such as title, character, issue number, publisher, or year published. It helps users quickly find comics of interest.

### Scope

* Users can view a list of all comics
* Users can search by title, character, issue number, publisher, or year published
* Users can click a comic to view detailed information
* Does not include checkout or user-specific actions

### Functional Requirements

1. **View Comics**

    * Display a list of all comics from the API (`GET /api/comics`)
    * Include key attributes: title, issue number, year published, publisher, and status

2. **Search / Filter**

    * Users can input search terms for title, character, issue number, publisher, or year published
    * System filters comics and updates list dynamically
    * Users can combine multiple filters
    * partial matches are supported
    * empty filters return all comics
    * filters are case-insensitive 

3. **View Details**

    * Users can select a comic from the list
    * System displays a detailed view including all comic attributes and associated characters

4. **Error Handling**

    * No results → display "No comics found"
    * API failure → display an error message

---

## Feature 3: Checkout Comic

### Overview / Purpose

This feature enables a logged-in user to borrow an available comic, creating a record in the system that tracks borrowing and due dates. It ensures a comic can only be borrowed by one user at a time.

### Scope

* Users must be logged in
* Only available comics can be checked out
* The system tracks the checkout in a database and updates comic status
* Does **not** handle reservations or overdue notifications

### Functional Requirements

1. **Checkout Process**

    * User selects a comic to borrow from the detail page
    * System verifies comic exists and is available
    * System sends request to API (`POST /api/checkouts`)
    * System creates a checkout record including: comic_id, user_id, checkout_date, due_date, and status = `"checked_out"`
    * Comic status is updated to `"unavailable"`

2. **Confirmation**

    * System displays a success message to the user

3. **Error Handling**

    * Comic is unavailable → show "Comic is already checked out"
    * Invalid request → show error message
    * API failure → show error message

---

## Feature 4: View Current Checkouts

### Overview / Purpose

This feature allows a logged-in user to view all comics they have currently borrowed. It provides visibility into active borrowings and due dates.

### Scope

* Only accessible to logged-in users
* Displays active checkout records (return_date = null)
* Does **not** include full checkout history of other users

### Functional Requirements

1. **View My Checkouts**

    * System calls API (`GET /api/checkouts/{id}`) using user’s JWT token
    * System displays each active checkout with: comic title, checkout date, due date, and status

2. **Detail Navigation**

    * User can click a comic to view its detail page

3. **Error Handling**

    * No active checkouts → display "No current checkouts"
    * API failure → display error message

---

## Feature 5: Return Comic (future enhancement)

### Description
Allows user to return a previously checked-out comic.

1. **Functional Requirements**

   * user an return a comic from the current checkouts page
   * system verifies the comic belongs to the user
   * system sets return_date
   * comic becomes available again
   * user receives confirmation message

---
