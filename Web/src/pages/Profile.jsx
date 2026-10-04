import {
  useEffect,
  useRef,
  useState,
} from "react";

import {
  useNavigate,
} from "react-router-dom";

import {
  useAuth,
} from "../context/AuthContext";

import api from "../services/api";

import UserAvatar
  from "../components/UserAvatar";

import "./Profile.css";

function Profile() {
  const {
    user,
    refreshUser,
  } = useAuth();

  const navigate =
    useNavigate();

  const fileInputRef =
    useRef(null);


  // ============================================================
  // FORM STATE
  // ============================================================

  const [
    form,
    setForm,
  ] = useState({
    fullName: "",
    phoneNumber: "",
    countryCode: "",
    region: "",
  });


  // ============================================================
  // UI STATE
  // ============================================================

  const [
    saving,
    setSaving,
  ] = useState(false);

  const [
    uploadingPhoto,
    setUploadingPhoto,
  ] = useState(false);

  const [
    removingPhoto,
    setRemovingPhoto,
  ] = useState(false);

  const [
    showRemovePhotoModal,
    setShowRemovePhotoModal,
  ] = useState(false);

  const [
    message,
    setMessage,
  ] = useState("");

  const [
    error,
    setError,
  ] = useState("");


  // ============================================================
  // LOAD CURRENT USER INTO FORM
  // ============================================================

  useEffect(() => {
    if (!user) {
      return;
    }

    setForm({
      fullName:
        user.fullName || "",

      phoneNumber:
        user.phoneNumber || "",

      countryCode:
        user.countryCode || "",

      region:
        user.region || "",
    });
  }, [user]);


  // ============================================================
  // HANDLE INPUT CHANGE
  // ============================================================

  const handleChange = (
    event
  ) => {
    const {
      name,
      value,
    } = event.target;

    setForm(
      (current) => ({
        ...current,
        [name]: value,
      })
    );
  };


  // ============================================================
  // SAVE PROFILE DETAILS
  // ============================================================

  const handleSave =
    async (event) => {
      event.preventDefault();

      setError("");
      setMessage("");

      if (
        !form.fullName.trim()
      ) {
        setError(
          "Please enter your full name."
        );

        return;
      }

      setSaving(true);

      try {
        const payload = {
          fullName:
            form.fullName.trim(),

          phoneNumber:
            form.phoneNumber.trim(),

          countryCode:
            form.countryCode
              .trim()
              .toUpperCase(),

          region:
            form.region.trim(),
        };

        await api.put(
          "/Auth/me/profile",
          payload
        );

        await refreshUser();

        setMessage(
          "Your profile has been updated successfully."
        );
      } catch (
        requestError
      ) {
        console.error(
          "Profile update failed:",
          requestError
        );

        setError(
          requestError
            ?.response
            ?.data
            ?.message ||
            "We couldn't update your profile. Please check your information and try again."
        );
      } finally {
        setSaving(false);
      }
    };


  // ============================================================
  // OPEN PHOTO PICKER
  // ============================================================

  const openFilePicker = () => {
    fileInputRef
      .current
      ?.click();
  };


  // ============================================================
  // UPLOAD PROFILE PHOTO
  // ============================================================

  const handlePhotoSelected =
    async (event) => {
      const file =
        event.target.files?.[0];

      event.target.value = "";

      if (!file) {
        return;
      }

      setError("");
      setMessage("");

      const allowedTypes = [
        "image/jpeg",
        "image/png",
        "image/webp",
      ];

      if (
        !allowedTypes.includes(
          file.type
        )
      ) {
        setError(
          "Please choose a JPG, PNG, or WebP image."
        );

        return;
      }

      if (
        file.size >
        5 * 1024 * 1024
      ) {
        setError(
          "Profile photos must be 5 MB or smaller."
        );

        return;
      }

      setUploadingPhoto(
        true
      );

      try {
        const formData =
          new FormData();

        formData.append(
          "file",
          file
        );

        await api.post(
          "/Auth/me/profile-image",
          formData
        );

        await refreshUser();

        setMessage(
          "Your profile photo has been updated successfully."
        );
      } catch (
        requestError
      ) {
        console.error(
          "Profile photo upload failed:",
          requestError
        );

        setError(
          requestError
            ?.response
            ?.data
            ?.message ||
            "We couldn't upload your profile photo. Please choose another image and try again."
        );
      } finally {
        setUploadingPhoto(
          false
        );
      }
    };


  // ============================================================
  // ASK TO REMOVE PROFILE PHOTO
  // ============================================================

  const requestRemovePhoto =
    () => {
      if (
        !user?.profileImageUrl
      ) {
        return;
      }

      setError("");
      setMessage("");

      setShowRemovePhotoModal(
        true
      );
    };


  // ============================================================
  // REMOVE PROFILE PHOTO
  // ============================================================

  const handleRemovePhoto =
    async () => {
      setShowRemovePhotoModal(
        false
      );

      setError("");
      setMessage("");

      setRemovingPhoto(
        true
      );

      try {
        await api.delete(
          "/Auth/me/profile-image"
        );

        await refreshUser();

        setMessage(
          "Your profile photo has been removed successfully."
        );
      } catch (
        requestError
      ) {
        console.error(
          "Profile photo removal failed:",
          requestError
        );

        setError(
          requestError
            ?.response
            ?.data
            ?.message ||
            "We couldn't remove your profile photo. Please try again."
        );
      } finally {
        setRemovingPhoto(
          false
        );
      }
    };


  // ============================================================
  // LOADING FALLBACK
  // ============================================================

  if (!user) {
    return null;
  }


  // ============================================================
  // UI
  // ============================================================

  return (
    <main className="gemora-profile-page">

      <div className="gemora-profile-shell">

        {/* ====================================================
            TOP NAVIGATION
            ==================================================== */}

        <div className="gemora-profile-topbar">

          <button
            type="button"
            className="gemora-profile-back"
            onClick={() =>
              navigate(
                "/dashboard"
              )
            }
          >
            ← Dashboard
          </button>

          <div className="gemora-profile-brand">
            GEMORA
          </div>

        </div>


        {/* ====================================================
            HERO
            ==================================================== */}

        <section className="gemora-profile-hero">

          <div>

            <span className="gemora-profile-eyebrow">
              MY ACCOUNT
            </span>

            <h1>
              Personal Profile
            </h1>

            <p>
              Manage your personal
              details and profile photo
              while your account role
              and verified email remain
              protected.
            </p>

          </div>


          <span className="gemora-profile-role-pill">
            {user.role}
          </span>

        </section>


        {/* ====================================================
            SUCCESS MESSAGE
            ==================================================== */}

        {message && (
          <div className="gemora-profile-success">

            <span>
              ✓
            </span>

            {message}

          </div>
        )}


        {/* ====================================================
            ERROR MESSAGE
            ==================================================== */}

        {error && (
          <div className="gemora-profile-error">

            <span>
              !
            </span>

            {error}

          </div>
        )}


        {/* ====================================================
            MAIN PROFILE LAYOUT
            ==================================================== */}

        <div className="gemora-profile-layout">


          {/* ==================================================
              LEFT SIDEBAR
              ================================================== */}

          <aside className="gemora-profile-sidebar">


            {/* ================================================
                PROFILE PHOTO
                ================================================ */}

            <section
              className="
                gemora-profile-card
                gemora-profile-photo-card
              "
            >

              <UserAvatar
                user={user}
                size={132}
              />


              <h2>
                {user.fullName}
              </h2>


              <p className="gemora-profile-email">
                {user.email}
              </p>


              <span className="gemora-profile-role">
                {user.role}
              </span>


              {/* HIDDEN FILE INPUT */}

              <input
                ref={
                  fileInputRef
                }
                type="file"
                accept="
                  image/jpeg,
                  image/png,
                  image/webp
                "
                hidden
                onChange={
                  handlePhotoSelected
                }
              />


              {/* UPLOAD / CHANGE PHOTO */}

              <button
                type="button"
                className="gemora-profile-photo-button"
                onClick={
                  openFilePicker
                }
                disabled={
                  uploadingPhoto ||
                  removingPhoto
                }
              >
                {uploadingPhoto
                  ? "Uploading..."
                  : user.profileImageUrl
                    ? "Change Photo"
                    : "Upload Photo"}
              </button>


              {/* REMOVE PHOTO */}

              {user.profileImageUrl && (
                <button
                  type="button"
                  className="gemora-profile-remove-button"
                  onClick={
                    requestRemovePhoto
                  }
                  disabled={
                    uploadingPhoto ||
                    removingPhoto
                  }
                >
                  {removingPhoto
                    ? "Removing..."
                    : "Remove Photo"}
                </button>
              )}


              <p className="gemora-profile-photo-help">
                JPG, PNG or WebP.
                Maximum file size 5 MB.
              </p>

            </section>


            {/* ================================================
                ACCOUNT STATUS
                ================================================ */}

            <section
              className="
                gemora-profile-card
                gemora-account-status-card
              "
            >

              <span className="gemora-profile-card-eyebrow">
                ACCOUNT STATUS
              </span>


              <div className="gemora-account-status-row">

                <span>
                  Email
                </span>

                <strong>
                  {user.isEmailVerified
                    ? "Verified"
                    : "Not Verified"}
                </strong>

              </div>


              <div className="gemora-account-status-row">

                <span>
                  Role
                </span>

                <strong>
                  {user.role}
                </strong>

              </div>


              <p>
                Your role and account
                permissions are managed
                securely by Gemora and
                cannot be changed from
                this page.
              </p>

            </section>

          </aside>


          {/* ==================================================
              PROFILE INFORMATION
              ================================================== */}

          <section
            className="
              gemora-profile-card
              gemora-profile-details-card
            "
          >

            <div className="gemora-profile-section-heading">

              <span className="gemora-profile-card-eyebrow">
                PERSONAL DETAILS
              </span>

              <h2>
                Profile Information
              </h2>

              <p>
                Keep your personal,
                contact and location
                information current.
              </p>

            </div>


            {/* ================================================
                EDIT PROFILE FORM
                ================================================ */}

            <form
              onSubmit={
                handleSave
              }
            >

              <div className="gemora-profile-form-grid">


                {/* FULL NAME */}

                <label
                  className="
                    gemora-profile-field
                    gemora-profile-field-wide
                  "
                >

                  <span>
                    Full Name
                  </span>

                  <input
                    type="text"
                    name="fullName"
                    value={
                      form.fullName
                    }
                    onChange={
                      handleChange
                    }
                    maxLength={100}
                    required
                  />

                </label>


                {/* PHONE NUMBER */}

                <label className="gemora-profile-field">

                  <span>
                    Phone Number
                  </span>

                  <input
                    type="tel"
                    name="phoneNumber"
                    value={
                      form.phoneNumber
                    }
                    onChange={
                      handleChange
                    }
                    placeholder="+94771234567"
                    maxLength={20}
                  />

                </label>


                {/* COUNTRY CODE */}

                <label className="gemora-profile-field">

                  <span>
                    Country Code
                  </span>

                  <input
                    type="text"
                    name="countryCode"
                    value={
                      form.countryCode
                    }
                    onChange={
                      handleChange
                    }
                    placeholder="LK"
                    maxLength={2}
                  />

                </label>


                {/* REGION */}

                <label
                  className="
                    gemora-profile-field
                    gemora-profile-field-wide
                  "
                >

                  <span>
                    Region
                  </span>

                  <input
                    type="text"
                    name="region"
                    value={
                      form.region
                    }
                    onChange={
                      handleChange
                    }
                    placeholder="North Western Province"
                    maxLength={100}
                  />

                </label>

              </div>


              {/* ==============================================
                  PROTECTED ACCOUNT INFORMATION
                  ============================================== */}

              <div className="gemora-profile-protected">


                <div className="gemora-profile-protected-heading">

                  <div>

                    <span className="gemora-profile-card-eyebrow">
                      PROTECTED ACCOUNT DETAILS
                    </span>

                    <h3>
                      Identity & Access
                    </h3>

                  </div>


                  <span
                    className="gemora-profile-lock"
                    aria-label="Protected"
                  >
                    🔒
                  </span>

                </div>


                <div className="gemora-profile-protected-grid">


                  {/* EMAIL */}

                  <label className="gemora-profile-field">

                    <span>
                      Email Address
                    </span>

                    <input
                      type="email"
                      value={
                        user.email ||
                        ""
                      }
                      readOnly
                    />

                    <small>
                      Email changes
                      require a separate
                      verification process.
                    </small>

                  </label>


                  {/* ROLE */}

                  <label className="gemora-profile-field">

                    <span>
                      Account Role
                    </span>

                    <input
                      type="text"
                      value={
                        user.role ||
                        ""
                      }
                      readOnly
                    />

                    <small>
                      Account roles are
                      controlled by Gemora.
                    </small>

                  </label>

                </div>

              </div>


              {/* ==============================================
                  SAVE BUTTON
                  ============================================== */}

              <div className="gemora-profile-actions">

                <button
                  type="submit"
                  className="gemora-profile-save"
                  disabled={
                    saving
                  }
                >
                  {saving
                    ? "Saving Changes..."
                    : "Save Changes"}
                </button>

              </div>

            </form>

          </section>

        </div>

      </div>


      {/* ======================================================
          REMOVE PROFILE PHOTO CONFIRMATION MODAL
          ====================================================== */}

      {showRemovePhotoModal && (

        <div
          className="gemora-modal-backdrop"
          role="presentation"
          onMouseDown={() =>
            setShowRemovePhotoModal(
              false
            )
          }
        >

          <div
            className="gemora-confirm-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="remove-photo-title"
            onMouseDown={(
              event
            ) =>
              event.stopPropagation()
            }
          >

            {/* ICON */}

            <div className="gemora-confirm-icon">
              !
            </div>


            {/* CONTENT */}

            <div className="gemora-confirm-content">

              <span className="gemora-confirm-eyebrow">
                PROFILE PHOTO
              </span>

              <h2 id="remove-photo-title">
                Remove profile photo?
              </h2>

              <p>
                Your current profile
                photo will be permanently
                removed from your Gemora
                account. Your initials
                will be shown instead.
              </p>

            </div>


            {/* ACTIONS */}

            <div className="gemora-confirm-actions">

              <button
                type="button"
                className="gemora-confirm-cancel"
                onClick={() =>
                  setShowRemovePhotoModal(
                    false
                  )
                }
              >
                Keep Photo
              </button>


              <button
                type="button"
                className="gemora-confirm-remove"
                onClick={
                  handleRemovePhoto
                }
                disabled={
                  removingPhoto
                }
              >
                {removingPhoto
                  ? "Removing..."
                  : "Remove Photo"}
              </button>

            </div>

          </div>

        </div>

      )}

    </main>
  );
}

export default Profile;