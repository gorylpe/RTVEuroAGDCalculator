
class Product {
  constructor(name, price) {
    this.Name = name;
    this.Price = price;
  }

  toString() {
    return this.Name;
  }
}

class Promotion {
  constructor(groupSize) {
    this.GroupSize = groupSize;
  }

  calculateForAscendingPrices(products) {
    throw new Error("Must be overridden by a subclass");
  }
}

class PromotionNone extends Promotion {
  constructor(groupSize) {
    super(groupSize);
  }

  calculateForAscendingPrices(products) {
    return products.map(product => product.Price);
  }
}

class PromotionCheapestDiscount extends Promotion {
  constructor(groupSize, discount) {
    super(groupSize);
    this._discount = discount;
  }

  calculateForAscendingPrices(products) {
    let first = true;
    return products.map(product => {
      if (first) {
        first = false;
        return product.Price * (1 - this._discount);
      } else {
        return product.Price;
      }
    });
  }
}

class PromotionCheapestFixedPrice extends Promotion {
  constructor(groupSize, fixedPrice) {
    super(groupSize);
    this._fixedPrice = fixedPrice;
  }

  calculateForAscendingPrices(products) {
    let first = true;
    return products.map(product => {
      if (first) {
        first = false;
        return this._fixedPrice;
      } else {
        return product.Price;
      }
    });
  }
}

class PromotionCombination {
  constructor() {
    this.Promotions = [];
    this.Sum = 0;
  }

  add(promotion) {
    this.Promotions.push(promotion);
    this.Sum += promotion.GroupSize;
  }

  copy() {
    const copy = new PromotionCombination();
    copy.Promotions = [...this.Promotions];  
    copy.Sum = this.Sum;
    return copy;
  }

  removeLast() {
    if (this.Promotions.length === 0)
        throw new Error("Trying to remove element from empty combination");
    this.Sum -= this.Promotions[this.Promotions.length - 1].GroupSize;
    this.Promotions.pop();
  }

  toString() {
    return `[${this.Promotions.map(x => x.GroupSize).join(",")}]`;
  }
}

function getBestSortedPromotions(products, promotions) {
  // Clone array products and promotions
  products = [...products];
  promotions = [...promotions];
  products.sort((x, y) => x.Price - y.Price);

  let combinations = [];
  findAllSortedCombinations(combinations, promotions, products.length);

  // Only combinations with all used promotions
  combinations = combinations.filter(x => {
    let count = 0;
    for (const y of x.Promotions) {
      if (y === promotions[0]) {
        count++;
      }
    }
    return count < promotions[1].GroupSize;
  });
  combinations.reverse();

  let promotionsWithTotalPrice = [];

  for (let c of combinations) {
    const promotionsPermutations = permuteWithoutRepetitions(c.Promotions);
    for (let promotionPermutation of promotionsPermutations) {
      let productsTaken = 0;
      let newPricesList = [];
      let totalPrice = 0.0;
      for (let promotion of promotionPermutation) {
        const productsGroup = products.slice(productsTaken, productsTaken + promotion.GroupSize);
        productsTaken += promotion.GroupSize;
        const newPrices = promotion.calculateForAscendingPrices(productsGroup);
        newPricesList.push(newPrices);
        totalPrice += newPrices.reduce((a, b) => a + b, 0);
      }
      promotionsWithTotalPrice.push([promotionPermutation, newPricesList, totalPrice]);
    }
  }

  promotionsWithTotalPrice.sort((x, y) => x[2] - y[2]);
  return promotionsWithTotalPrice;
}

function findAllSortedCombinations(combinations, promotions, products) {
  let tmpCombination = new PromotionCombination();
  findAllSortedCombinationsInternal(combinations, promotions, products, tmpCombination, 0);
}

function findAllSortedCombinationsInternal(combinations, promotions, products, c, minPromotionIndex) {
  for (let i = minPromotionIndex; i < promotions.length; i++) {
    try {
      const promotion = promotions[i];
      c.add(promotion);

      if (c.Sum > products)
          return;
      else if (c.Sum == products) {
          combinations.push(c.copy());
          return;
      }

      findAllSortedCombinationsInternal(combinations, promotions, products, c, i);
    } finally {
        c.removeLast();
    }
  }
}

function permuteWithoutRepetitions(arr) {
  let permutations = [];
  permuteWithoutRepetitionsInternal(permutations, arr, arr.length - 1, 0);
  return permutations;
}

function permuteWithoutRepetitionsInternal(permutations, arr, end, start) {
  permutations.push([...arr]);

  for (let left = end - 1; left >= start; left--) {
      for (let right = left + 1; right <= end; right++) {
          if (arr[left] !== arr[right]) {
              swap(arr, left, right);
              permuteWithoutRepetitionsInternal(permutations, arr, end, left + 1);
          }
      }

      let firstElement = arr[left];
      for (let i = left; i <= end - 1; i++)
          arr[i] = arr[i + 1];

      arr[end] = firstElement;
  }
}

function swap(arr, a, b) {
  if (arr[a] === arr[b]) return false;
  [arr[b], arr[a]] = [arr[a], arr[b]];
  return true;
}

// const promotions = [
//     new PromotionNone(1),
//     new PromotionCheapestDiscount(2, 0.3),
//     new PromotionCheapestDiscount(3, 0.55),
//     new PromotionCheapestDiscount(4, 0.8),
//     new PromotionCheapestFixedPrice(5, 1)
//   ];
// promotions.sort((x, y) => x.GroupSize - y.GroupSize);

// const products = [
//   new Product("Pralka", 3397),
//   new Product("Suszarka", 4291),
//   new Product("Piekarnik", 4469),
//   new Product("Indukcja", 1789),
//   new Product("Okap", 1239),
//   new Product("Zmywarka", 3349),
//   new Product("Lodówka", 3499),
//   new Product("Mikrofala", 2290),
//   // new Product("Telewizor", 8000),
// ];

// const promotionsWithTotalPrice = getBestSortedPromotions(products, promotions);
// console.log(`Total permutations ${promotionsWithTotalPrice.length}`)
// for (let p of promotionsWithTotalPrice) {
//   console.log(`Permutation [${p[0].map(x => x.GroupSize).join(",")}], total price - ${p[2].toFixed(2)}`);
//   const formattedPrices = p[1].map(np => `[${np.map(x => x.toFixed(2)).join(";")}]`).join(" ");
//   console.log(formattedPrices);
//   console.log("");
// }