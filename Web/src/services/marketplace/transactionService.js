import api from "../api";

const transactionService = {
  async getAll() {
    const response = await api.get("/admin/transactions");
    return response.data;
  },
};

export default transactionService;
