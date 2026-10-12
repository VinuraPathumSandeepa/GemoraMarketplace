import api from "./api";

const adminGemListingService = {
  async getAll() {
    const response = await api.get("/admin/gem-listings");
    return response.data;
  },

  async remove(id) {
    await api.delete(`/admin/gem-listings/${id}`);
  },
};

export default adminGemListingService;
