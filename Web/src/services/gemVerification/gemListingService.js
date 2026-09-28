import api from "../api";

const gemListingService = {
  // Get all listings owned by the currently logged-in seller
  getMyListings: async () => {
    const response = await api.get("/GemListings/my");
    return response.data;
  },

  // Get one seller-owned listing
  getListingById: async (id) => {
    const response = await api.get(`/GemListings/${id}`);
    return response.data;
  },

  // Create a new Draft listing
  createListing: async (listingData) => {
    const response = await api.post("/GemListings", listingData);
    return response.data;
  },

  // Update Draft / ChangesRequested listing
  updateListing: async (id, listingData) => {
    const response = await api.put(
      `/GemListings/${id}`,
      listingData
    );

    return response.data;
  },

  // Delete a Draft listing
  deleteListing: async (id) => {
    await api.delete(`/GemListings/${id}`);
  },

  // Submit Draft / ChangesRequested listing
  // to the Gemologist verification workflow
  submitListing: async (id) => {
    const response = await api.post(
      `/GemListings/${id}/submit`
    );

    return response.data;
  },

  // Upload actual gemstone photograph
  uploadImage: async (id, file) => {
    const formData = new FormData();
    formData.append("file", file);

    const response = await api.post(
      `/GemListings/${id}/image`,
      formData,
      {
        headers: {
          "Content-Type": "multipart/form-data",
        },
      }
    );

    return response.data;
  },

  // Upload certificate PDF/image
  uploadCertificate: async (id, file) => {
    const formData = new FormData();
    formData.append("file", file);

    const response = await api.post(
      `/GemListings/${id}/certificate`,
      formData,
      {
        headers: {
          "Content-Type": "multipart/form-data",
        },
      }
    );

    return response.data;
  },
};

export default gemListingService;