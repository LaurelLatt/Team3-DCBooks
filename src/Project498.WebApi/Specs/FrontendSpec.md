# Frontend Specification

## Overview

The frontend is a web-based user interface built using Bootstrap. It allows users to browse comics, manage accounts, and interact with the backend services.

---

## Pages

### Home / Comics Page

* Displays list of all comics
* Includes search and filter functionality:

    * Title
    * Character
    * Issue Number
    * Publisher
    * Year
* Each comic is clickable and navigates to the detail page

---

### Comic Detail Page

* Displays full comic information:

    * Title
    * Issue Number
    * Publisher
    * Year
    * Characters
    * Availability status
  
* Show "Checkout" button if available
* If unavailable:

    * Display "Checked out" status

---

### Login Page

* Form fields:

    * Username
    * Password
* Submits login request to backend
* Displays error messages for invalid login

---

### Registration Page

* Form fields:

    * First name
    * Last name
    * Username
    * Email
    * Password
* Validates input before submission
* Ensures no missing information
---


## UI Requirements

* Use Bootstrap for styling and layout
* Responsive design for different screen sizes
* Clear navigation between pages

---
